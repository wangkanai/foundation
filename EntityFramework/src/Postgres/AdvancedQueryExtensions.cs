// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using System.Linq.Expressions;

namespace Wangkanai.EntityFramework.Postgres;

/// <summary>
/// Provides extension methods for advanced PostgreSQL query capabilities including
/// Common Table Expressions (CTEs), Materialized Views, Window Functions, and
/// complex analytical operations optimized for PostgreSQL.
/// </summary>
public static class AdvancedQueryExtensions
{
    #region Common Table Expressions (CTEs)

    /// <summary>
    /// Executes a query using a PostgreSQL Common Table Expression (CTE) for improved readability and performance.
    /// CTEs provide a way to define temporary result sets that can be referenced within the main query.
    /// </summary>
    /// <typeparam name="T">The entity type for the query result.</typeparam>
    /// <param name="context">The DbContext to execute the query against.</param>
    /// <param name="cteName">Name for the CTE (Common Table Expression).</param>
    /// <param name="cteQuery">The CTE query SQL.</param>
    /// <param name="mainQuery">The main query that references the CTE.</param>
    /// <returns>A queryable result set from the CTE query.</returns>
    /// <exception cref="ArgumentException">Thrown when parameters are null or whitespace.</exception>
    /// <example>
    /// <code>
    /// // Simple CTE for department statistics
    /// var departmentStats = context.ExecuteWithCte&lt;DepartmentStats&gt;(
    ///     cteName: "dept_stats",
    ///     cteQuery: @"SELECT 
    ///                   department_id,
    ///                   COUNT(*) as employee_count,
    ///                   AVG(salary) as avg_salary
    ///                 FROM employees 
    ///                 GROUP BY department_id",
    ///     mainQuery: @"SELECT d.name as department_name,
    ///                         ds.employee_count,
    ///                         ds.avg_salary
    ///                  FROM dept_stats ds
    ///                  JOIN departments d ON d.id = ds.department_id
    ///                  ORDER BY ds.avg_salary DESC");
    /// 
    /// // Generated SQL:
    /// // WITH dept_stats AS (
    /// //   SELECT department_id, COUNT(*) as employee_count, AVG(salary) as avg_salary
    /// //   FROM employees GROUP BY department_id
    /// // )
    /// // SELECT d.name as department_name, ds.employee_count, ds.avg_salary
    /// // FROM dept_stats ds
    /// // JOIN departments d ON d.id = ds.department_id
    /// // ORDER BY ds.avg_salary DESC;
    /// </code>
    /// </example>
    public static IQueryable<T> ExecuteWithCte<T>(
        this DbContext context,
        string cteName,
        string cteQuery,
        string mainQuery) where T : class
    {
        if (string.IsNullOrWhiteSpace(cteName))
            throw new ArgumentException("CTE name cannot be null or whitespace.", nameof(cteName));
        if (string.IsNullOrWhiteSpace(cteQuery))
            throw new ArgumentException("CTE query cannot be null or whitespace.", nameof(cteQuery));
        if (string.IsNullOrWhiteSpace(mainQuery))
            throw new ArgumentException("Main query cannot be null or whitespace.", nameof(mainQuery));

        var fullQuery = $"WITH {cteName} AS ({cteQuery}) {mainQuery}";
        return context.Set<T>().FromSqlRaw(fullQuery);
    }

    /// <summary>
    /// Executes a recursive CTE query for hierarchical data structures like organizational charts or category trees.
    /// Recursive CTEs are powerful for traversing tree-like data structures in a single query.
    /// </summary>
    /// <typeparam name="T">The entity type for the query result.</typeparam>
    /// <param name="context">The DbContext to execute the query against.</param>
    /// <param name="cteName">Name for the recursive CTE.</param>
    /// <param name="anchorQuery">The anchor query (base case for recursion).</param>
    /// <param name="recursiveQuery">The recursive query (recursive case).</param>
    /// <param name="mainQuery">The main query that references the recursive CTE.</param>
    /// <param name="maxRecursionDepth">Optional maximum recursion depth to prevent infinite loops.</param>
    /// <returns>A queryable result set from the recursive CTE query.</returns>
    /// <exception cref="ArgumentException">Thrown when parameters are null or whitespace.</exception>
    /// <example>
    /// <code>
    /// // Recursive CTE for organizational hierarchy
    /// var orgHierarchy = context.ExecuteRecursiveCte&lt;EmployeeHierarchy&gt;(
    ///     cteName: "employee_hierarchy",
    ///     anchorQuery: @"SELECT 
    ///                      id, name, manager_id, 0 as level, 
    ///                      CAST(name AS VARCHAR(1000)) as path
    ///                    FROM employees 
    ///                    WHERE manager_id IS NULL",
    ///     recursiveQuery: @"SELECT 
    ///                         e.id, e.name, e.manager_id, eh.level + 1,
    ///                         CAST(eh.path || ' -> ' || e.name AS VARCHAR(1000))
    ///                       FROM employees e
    ///                       INNER JOIN employee_hierarchy eh ON e.manager_id = eh.id",
    ///     mainQuery: @"SELECT id, name, level, path 
    ///                  FROM employee_hierarchy 
    ///                  ORDER BY level, name",
    ///     maxRecursionDepth: 10);
    /// 
    /// // Generated SQL:
    /// // WITH RECURSIVE employee_hierarchy AS (
    /// //   -- Anchor: Find top-level managers
    /// //   SELECT id, name, manager_id, 0 as level, CAST(name AS VARCHAR(1000)) as path
    /// //   FROM employees WHERE manager_id IS NULL
    /// //   
    /// //   UNION ALL
    /// //   
    /// //   -- Recursive: Find direct reports
    /// //   SELECT e.id, e.name, e.manager_id, eh.level + 1,
    /// //          CAST(eh.path || ' -> ' || e.name AS VARCHAR(1000))
    /// //   FROM employees e
    /// //   INNER JOIN employee_hierarchy eh ON e.manager_id = eh.id
    /// //   WHERE eh.level &lt; 10
    /// // )
    /// // SELECT id, name, level, path FROM employee_hierarchy ORDER BY level, name;
    /// </code>
    /// </example>
    public static IQueryable<T> ExecuteRecursiveCte<T>(
        this DbContext context,
        string cteName,
        string anchorQuery,
        string recursiveQuery,
        string mainQuery,
        int? maxRecursionDepth = null) where T : class
    {
        if (string.IsNullOrWhiteSpace(cteName))
            throw new ArgumentException("CTE name cannot be null or whitespace.", nameof(cteName));
        if (string.IsNullOrWhiteSpace(anchorQuery))
            throw new ArgumentException("Anchor query cannot be null or whitespace.", nameof(anchorQuery));
        if (string.IsNullOrWhiteSpace(recursiveQuery))
            throw new ArgumentException("Recursive query cannot be null or whitespace.", nameof(recursiveQuery));
        if (string.IsNullOrWhiteSpace(mainQuery))
            throw new ArgumentException("Main query cannot be null or whitespace.", nameof(mainQuery));

        var recursionLimit = maxRecursionDepth.HasValue ? $" WHERE level < {maxRecursionDepth.Value}" : "";
        var modifiedRecursiveQuery = recursiveQuery + recursionLimit;

        var fullQuery = $@"WITH RECURSIVE {cteName} AS (
            {anchorQuery}
            UNION ALL
            {modifiedRecursiveQuery}
        ) {mainQuery}";

        return context.Set<T>().FromSqlRaw(fullQuery);
    }

    /// <summary>
    /// Executes a multiple CTE query for complex data processing with multiple temporary result sets.
    /// Supports chaining multiple CTEs for sophisticated data transformations and analysis.
    /// </summary>
    /// <typeparam name="T">The entity type for the query result.</typeparam>
    /// <param name="context">The DbContext to execute the query against.</param>
    /// <param name="cteDefinitions">Dictionary of CTE names and their corresponding queries.</param>
    /// <param name="mainQuery">The main query that references the CTEs.</param>
    /// <returns>A queryable result set from the multiple CTE query.</returns>
    /// <exception cref="ArgumentException">Thrown when parameters are null or empty.</exception>
    /// <example>
    /// <code>
    /// // Multiple CTEs for complex sales analysis
    /// var complexSalesAnalysis = context.ExecuteMultipleCte&lt;SalesAnalysisResult&gt;(
    ///     cteDefinitions: new Dictionary&lt;string, string&gt;
    ///     {
    ///         ["monthly_sales"] = @"SELECT 
    ///                                DATE_TRUNC('month', sale_date) as month,
    ///                                SUM(amount) as total_sales,
    ///                                COUNT(*) as transaction_count
    ///                              FROM sales 
    ///                              WHERE sale_date >= '2023-01-01'
    ///                              GROUP BY DATE_TRUNC('month', sale_date)",
    ///         
    ///         ["customer_stats"] = @"SELECT 
    ///                                 customer_id,
    ///                                 SUM(amount) as customer_total,
    ///                                 COUNT(*) as purchase_count,
    ///                                 AVG(amount) as avg_purchase
    ///                               FROM sales 
    ///                               WHERE sale_date >= '2023-01-01'
    ///                               GROUP BY customer_id",
    ///         
    ///         ["top_customers"] = @"SELECT 
    ///                                customer_id, customer_total
    ///                              FROM customer_stats 
    ///                              WHERE customer_total > 1000
    ///                              ORDER BY customer_total DESC
    ///                              LIMIT 100"
    ///     },
    ///     mainQuery: @"SELECT 
    ///                    ms.month,
    ///                    ms.total_sales,
    ///                    ms.transaction_count,
    ///                    COUNT(tc.customer_id) as top_customers_count
    ///                  FROM monthly_sales ms
    ///                  LEFT JOIN top_customers tc ON DATE_TRUNC('month', ms.month) = DATE_TRUNC('month', ms.month)
    ///                  GROUP BY ms.month, ms.total_sales, ms.transaction_count
    ///                  ORDER BY ms.month");
    /// 
    /// // Generated SQL:
    /// // WITH 
    /// // monthly_sales AS (SELECT DATE_TRUNC('month', sale_date) as month, ...),
    /// // customer_stats AS (SELECT customer_id, SUM(amount) as customer_total, ...),
    /// // top_customers AS (SELECT customer_id, customer_total FROM customer_stats WHERE ...)
    /// // SELECT ms.month, ms.total_sales, ms.transaction_count, COUNT(tc.customer_id)
    /// // FROM monthly_sales ms LEFT JOIN top_customers tc ON ...
    /// </code>
    /// </example>
    public static IQueryable<T> ExecuteMultipleCte<T>(
        this DbContext context,
        Dictionary<string, string> cteDefinitions,
        string mainQuery) where T : class
    {
        if (cteDefinitions == null || cteDefinitions.Count == 0)
            throw new ArgumentException("CTE definitions cannot be null or empty.", nameof(cteDefinitions));
        if (string.IsNullOrWhiteSpace(mainQuery))
            throw new ArgumentException("Main query cannot be null or whitespace.", nameof(mainQuery));

        var cteList = string.Join(",\n", cteDefinitions.Select(kv => $"{kv.Key} AS ({kv.Value})"));
        var fullQuery = $"WITH {cteList} {mainQuery}";

        return context.Set<T>().FromSqlRaw(fullQuery);
    }

    #endregion

    #region Materialized Views

    /// <summary>
    /// Creates a materialized view for caching complex query results with configurable refresh strategies.
    /// Materialized views store query results physically, providing fast access to frequently accessed complex data.
    /// </summary>
    /// <param name="context">The DbContext to execute against.</param>
    /// <param name="viewName">Name of the materialized view.</param>
    /// <param name="query">SQL query for the materialized view.</param>
    /// <param name="refreshStrategy">Strategy for refreshing the materialized view.</param>
    /// <param name="tablespace">Optional tablespace for storing the materialized view.</param>
    /// <param name="withData">Whether to populate the view with data immediately.</param>
    /// <returns>The number of rows affected by the operation.</returns>
    /// <exception cref="ArgumentException">Thrown when parameters are null or whitespace.</exception>
    /// <example>
    /// <code>
    /// // Create materialized view for daily sales summary
    /// await context.CreateMaterializedViewAsync(
    ///     viewName: "daily_sales_summary",
    ///     query: @"SELECT 
    ///                DATE(sale_timestamp) as sale_date,
    ///                COUNT(*) as transaction_count,
    ///                SUM(amount) as total_sales,
    ///                AVG(amount) as avg_transaction,
    ///                COUNT(DISTINCT customer_id) as unique_customers
    ///              FROM sales 
    ///              GROUP BY DATE(sale_timestamp)",
    ///     refreshStrategy: MaterializedViewRefreshStrategy.Manual,
    ///     tablespace: "analytics_tbs",
    ///     withData: true);
    /// 
    /// // Generated SQL:
    /// // CREATE MATERIALIZED VIEW daily_sales_summary TABLESPACE analytics_tbs AS
    /// // SELECT DATE(sale_timestamp) as sale_date, COUNT(*) as transaction_count,
    /// //        SUM(amount) as total_sales, AVG(amount) as avg_transaction,
    /// //        COUNT(DISTINCT customer_id) as unique_customers
    /// // FROM sales GROUP BY DATE(sale_timestamp)
    /// // WITH DATA;
    /// 
    /// // Query the materialized view for fast results:
    /// // SELECT * FROM daily_sales_summary WHERE sale_date >= '2023-01-01';
    /// </code>
    /// </example>
    public static async Task<int> CreateMaterializedViewAsync(
        this DbContext context,
        string viewName,
        string query,
        MaterializedViewRefreshStrategy refreshStrategy = MaterializedViewRefreshStrategy.Manual,
        string? tablespace = null,
        bool withData = true)
    {
        if (string.IsNullOrWhiteSpace(viewName))
            throw new ArgumentException("View name cannot be null or whitespace.", nameof(viewName));
        if (string.IsNullOrWhiteSpace(query))
            throw new ArgumentException("Query cannot be null or whitespace.", nameof(query));

        var tablespaceClause = !string.IsNullOrWhiteSpace(tablespace) ? $" TABLESPACE {tablespace}" : "";
        var withDataClause = withData ? " WITH DATA" : " WITH NO DATA";
        
        var createViewSql = $"CREATE MATERIALIZED VIEW {viewName}{tablespaceClause} AS {query}{withDataClause}";
        
        var result = await context.Database.ExecuteSqlRawAsync(createViewSql);

        // Set up refresh strategy if specified
        if (refreshStrategy != MaterializedViewRefreshStrategy.Manual)
        {
            await ConfigureMaterializedViewRefreshAsync(context, viewName, refreshStrategy);
        }

        return result;
    }

    /// <summary>
    /// Refreshes a materialized view with options for concurrent refresh and specific refresh strategies.
    /// Provides control over how and when materialized views are updated with fresh data.
    /// </summary>
    /// <param name="context">The DbContext to execute against.</param>
    /// <param name="viewName">Name of the materialized view to refresh.</param>
    /// <param name="concurrent">Whether to use concurrent refresh (allows reads during refresh).</param>
    /// <param name="refreshType">Type of refresh to perform.</param>
    /// <returns>The number of rows affected by the refresh operation.</returns>
    /// <exception cref="ArgumentException">Thrown when viewName is null or whitespace.</exception>
    /// <example>
    /// <code>
    /// // Concurrent refresh for high-availability scenarios
    /// await context.RefreshMaterializedViewAsync(
    ///     viewName: "daily_sales_summary",
    ///     concurrent: true,
    ///     refreshType: MaterializedViewRefreshType.Complete);
    /// 
    /// // Generated SQL:
    /// // REFRESH MATERIALIZED VIEW CONCURRENTLY daily_sales_summary;
    /// 
    /// // Non-concurrent refresh for maximum performance
    /// await context.RefreshMaterializedViewAsync(
    ///     viewName: "monthly_analytics",
    ///     concurrent: false,
    ///     refreshType: MaterializedViewRefreshType.Complete);
    /// 
    /// // Generated SQL:
    /// // REFRESH MATERIALIZED VIEW monthly_analytics;
    /// 
    /// // Benefits:
    /// // - Concurrent: Allows SELECT queries during refresh (requires unique index)
    /// // - Non-concurrent: Faster refresh but locks view for reads during operation
    /// </code>
    /// </example>
    public static async Task<int> RefreshMaterializedViewAsync(
        this DbContext context,
        string viewName,
        bool concurrent = false,
        MaterializedViewRefreshType refreshType = MaterializedViewRefreshType.Complete)
    {
        if (string.IsNullOrWhiteSpace(viewName))
            throw new ArgumentException("View name cannot be null or whitespace.", nameof(viewName));

        var concurrentClause = concurrent ? "CONCURRENTLY " : "";
        var refreshSql = $"REFRESH MATERIALIZED VIEW {concurrentClause}{viewName}";

        return await context.Database.ExecuteSqlRawAsync(refreshSql);
    }

    /// <summary>
    /// Configures automatic refresh for materialized views using PostgreSQL's pg_cron extension.
    /// Enables scheduled refresh operations for keeping materialized views up-to-date automatically.
    /// </summary>
    /// <param name="context">The DbContext to execute against.</param>
    /// <param name="viewName">Name of the materialized view.</param>
    /// <param name="refreshStrategy">Refresh strategy configuration.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <example>
    /// <code>
    /// // Configure hourly refresh for real-time analytics
    /// await context.ConfigureMaterializedViewRefreshAsync(
    ///     viewName: "hourly_metrics",
    ///     refreshStrategy: MaterializedViewRefreshStrategy.Hourly);
    /// 
    /// // Configure daily refresh for summary reports
    /// await context.ConfigureMaterializedViewRefreshAsync(
    ///     viewName: "daily_summary",
    ///     refreshStrategy: MaterializedViewRefreshStrategy.Daily);
    /// 
    /// // Generated SQL:
    /// // SELECT cron.schedule('refresh_hourly_metrics', '0 * * * *', 
    /// //   'REFRESH MATERIALIZED VIEW CONCURRENTLY hourly_metrics;');
    /// // SELECT cron.schedule('refresh_daily_summary', '0 2 * * *',
    /// //   'REFRESH MATERIALIZED VIEW CONCURRENTLY daily_summary;');
    /// 
    /// // Note: Requires pg_cron extension to be installed and enabled
    /// </code>
    /// </example>
    public static async Task ConfigureMaterializedViewRefreshAsync(
        this DbContext context,
        string viewName,
        MaterializedViewRefreshStrategy refreshStrategy)
    {
        if (string.IsNullOrWhiteSpace(viewName))
            throw new ArgumentException("View name cannot be null or whitespace.", nameof(viewName));

        var (cronExpression, jobName) = refreshStrategy switch
        {
            MaterializedViewRefreshStrategy.Hourly => ("0 * * * *", $"refresh_{viewName}_hourly"),
            MaterializedViewRefreshStrategy.Daily => ("0 2 * * *", $"refresh_{viewName}_daily"),
            MaterializedViewRefreshStrategy.Weekly => ("0 2 * * 0", $"refresh_{viewName}_weekly"),
            MaterializedViewRefreshStrategy.Monthly => ("0 2 1 * *", $"refresh_{viewName}_monthly"),
            MaterializedViewRefreshStrategy.Custom => throw new ArgumentException("Custom refresh strategy requires manual configuration."),
            _ => throw new ArgumentException($"Unsupported refresh strategy: {refreshStrategy}")
        };

        var scheduleSql = $@"
            SELECT cron.schedule(
                '{jobName}', 
                '{cronExpression}', 
                'REFRESH MATERIALIZED VIEW CONCURRENTLY {viewName};'
            );";

        await context.Database.ExecuteSqlRawAsync(scheduleSql);
    }

    /// <summary>
    /// Drops a materialized view and associated refresh schedules.
    /// Provides clean removal of materialized views including their maintenance jobs.
    /// </summary>
    /// <param name="context">The DbContext to execute against.</param>
    /// <param name="viewName">Name of the materialized view to drop.</param>
    /// <param name="cascade">Whether to drop dependent objects.</param>
    /// <returns>The number of rows affected by the operation.</returns>
    /// <exception cref="ArgumentException">Thrown when viewName is null or whitespace.</exception>
    /// <example>
    /// <code>
    /// // Drop materialized view and cleanup scheduled jobs
    /// await context.DropMaterializedViewAsync("outdated_summary", cascade: true);
    /// 
    /// // Generated SQL:
    /// // SELECT cron.unschedule('refresh_outdated_summary_daily');
    /// // DROP MATERIALIZED VIEW IF EXISTS outdated_summary CASCADE;
    /// </code>
    /// </example>
    public static async Task<int> DropMaterializedViewAsync(
        this DbContext context,
        string viewName,
        bool cascade = false)
    {
        if (string.IsNullOrWhiteSpace(viewName))
            throw new ArgumentException("View name cannot be null or whitespace.", nameof(viewName));

        // Try to unschedule any associated cron jobs
        var unscheduleSql = $@"
            SELECT cron.unschedule(job_name) 
            FROM cron.job 
            WHERE job_name LIKE 'refresh_{viewName}_%';";
        
        try
        {
            await context.Database.ExecuteSqlRawAsync(unscheduleSql);
        }
        catch
        {
            // Ignore errors if pg_cron is not available or jobs don't exist
        }

        var cascadeClause = cascade ? " CASCADE" : "";
        var dropSql = $"DROP MATERIALIZED VIEW IF EXISTS {viewName}{cascadeClause}";

        return await context.Database.ExecuteSqlRawAsync(dropSql);
    }

    #endregion

    #region Window Functions

    /// <summary>
    /// Executes a query with PostgreSQL window functions for advanced analytical operations.
    /// Window functions perform calculations across related rows without collapsing the result set.
    /// </summary>
    /// <typeparam name="T">The entity type for the query result.</typeparam>
    /// <param name="context">The DbContext to execute the query against.</param>
    /// <param name="query">SQL query containing window functions.</param>
    /// <returns>A queryable result set with window function calculations.</returns>
    /// <exception cref="ArgumentException">Thrown when query is null or whitespace.</exception>
    /// <example>
    /// <code>
    /// // Window functions for sales ranking and running totals
    /// var salesAnalytics = context.ExecuteWindowFunction&lt;SalesAnalytics&gt;(@"
    ///     SELECT 
    ///         salesperson_id,
    ///         sale_date,
    ///         amount,
    ///         -- Row number within each salesperson's sales
    ///         ROW_NUMBER() OVER (PARTITION BY salesperson_id ORDER BY sale_date) as sale_sequence,
    ///         
    ///         -- Rank sales amount within each month
    ///         RANK() OVER (PARTITION BY DATE_TRUNC('month', sale_date) ORDER BY amount DESC) as monthly_rank,
    ///         
    ///         -- Running total of sales for each salesperson
    ///         SUM(amount) OVER (PARTITION BY salesperson_id ORDER BY sale_date 
    ///                          ROWS UNBOUNDED PRECEDING) as running_total,
    ///         
    ///         -- Moving average of last 7 days
    ///         AVG(amount) OVER (PARTITION BY salesperson_id ORDER BY sale_date 
    ///                          ROWS 6 PRECEDING) as moving_avg_7d,
    ///         
    ///         -- Previous sale amount for comparison
    ///         LAG(amount, 1) OVER (PARTITION BY salesperson_id ORDER BY sale_date) as previous_sale,
    ///         
    ///         -- Next sale amount for comparison
    ///         LEAD(amount, 1) OVER (PARTITION BY salesperson_id ORDER BY sale_date) as next_sale,
    ///         
    ///         -- Percentage of total monthly sales
    ///         amount / SUM(amount) OVER (PARTITION BY DATE_TRUNC('month', sale_date)) * 100 as pct_of_monthly_total
    ///     FROM sales 
    ///     WHERE sale_date >= '2023-01-01'
    ///     ORDER BY salesperson_id, sale_date
    /// ");
    /// 
    /// // Enables sophisticated analytics like:
    /// // - Sales performance ranking
    /// // - Trend analysis with moving averages
    /// // - Period-over-period comparisons
    /// // - Running totals and cumulative metrics
    /// </code>
    /// </example>
    public static IQueryable<T> ExecuteWindowFunction<T>(
        this DbContext context,
        string query) where T : class
    {
        if (string.IsNullOrWhiteSpace(query))
            throw new ArgumentException("Query cannot be null or whitespace.", nameof(query));

        return context.Set<T>().FromSqlRaw(query);
    }

    /// <summary>
    /// Executes ranking window functions (ROW_NUMBER, RANK, DENSE_RANK, PERCENT_RANK) for competitive analysis.
    /// Provides standardized ranking operations with configurable partitioning and ordering.
    /// </summary>
    /// <typeparam name="T">The entity type for the query result.</typeparam>
    /// <param name="context">The DbContext to execute the query against.</param>
    /// <param name="tableName">Name of the table to query.</param>
    /// <param name="rankingColumn">Column to rank by.</param>
    /// <param name="partitionColumns">Columns to partition the ranking by.</param>
    /// <param name="orderDirection">Order direction for ranking (ASC or DESC).</param>
    /// <param name="includeAllRankTypes">Include all ranking function types.</param>
    /// <param name="additionalColumns">Additional columns to include in the result.</param>
    /// <returns>A queryable result set with ranking calculations.</returns>
    /// <exception cref="ArgumentException">Thrown when parameters are null or whitespace.</exception>
    /// <example>
    /// <code>
    /// // Employee salary rankings by department
    /// var salaryRankings = context.ExecuteRankingFunction&lt;EmployeeRanking&gt;(
    ///     tableName: "employees",
    ///     rankingColumn: "salary",
    ///     partitionColumns: new[] { "department_id" },
    ///     orderDirection: "DESC",
    ///     includeAllRankTypes: true,
    ///     additionalColumns: new[] { "employee_id", "first_name", "last_name", "department_id", "salary" });
    /// 
    /// // Generated SQL:
    /// // SELECT 
    /// //   employee_id, first_name, last_name, department_id, salary,
    /// //   ROW_NUMBER() OVER (PARTITION BY department_id ORDER BY salary DESC) as row_number,
    /// //   RANK() OVER (PARTITION BY department_id ORDER BY salary DESC) as rank,
    /// //   DENSE_RANK() OVER (PARTITION BY department_id ORDER BY salary DESC) as dense_rank,
    /// //   PERCENT_RANK() OVER (PARTITION BY department_id ORDER BY salary DESC) as percent_rank
    /// // FROM employees
    /// // ORDER BY department_id, salary DESC;
    /// 
    /// // Results provide:
    /// // - Unique sequential numbers (ROW_NUMBER)
    /// // - Standard competition ranking (RANK) 
    /// // - Dense ranking without gaps (DENSE_RANK)
    /// // - Percentile ranking (PERCENT_RANK)
    /// </code>
    /// </example>
    public static IQueryable<T> ExecuteRankingFunction<T>(
        this DbContext context,
        string tableName,
        string rankingColumn,
        string[] partitionColumns,
        string orderDirection = "DESC",
        bool includeAllRankTypes = false,
        string[]? additionalColumns = null) where T : class
    {
        if (string.IsNullOrWhiteSpace(tableName))
            throw new ArgumentException("Table name cannot be null or whitespace.", nameof(tableName));
        if (string.IsNullOrWhiteSpace(rankingColumn))
            throw new ArgumentException("Ranking column cannot be null or whitespace.", nameof(rankingColumn));
        if (partitionColumns == null || partitionColumns.Length == 0)
            throw new ArgumentException("Partition columns cannot be null or empty.", nameof(partitionColumns));

        var partitionBy = string.Join(", ", partitionColumns);
        var orderBy = $"{rankingColumn} {orderDirection}";
        var windowClause = $"PARTITION BY {partitionBy} ORDER BY {orderBy}";

        var selectColumns = new List<string>();
        if (additionalColumns != null && additionalColumns.Length > 0)
        {
            selectColumns.AddRange(additionalColumns);
        }
        else
        {
            selectColumns.Add("*");
        }

        var rankingFunctions = new List<string>
        {
            $"ROW_NUMBER() OVER ({windowClause}) as row_number"
        };

        if (includeAllRankTypes)
        {
            rankingFunctions.AddRange(new[]
            {
                $"RANK() OVER ({windowClause}) as rank",
                $"DENSE_RANK() OVER ({windowClause}) as dense_rank",
                $"PERCENT_RANK() OVER ({windowClause}) as percent_rank"
            });
        }

        var allColumns = string.Join(", ", selectColumns.Concat(rankingFunctions));
        var query = $@"
            SELECT {allColumns}
            FROM {tableName}
            ORDER BY {partitionBy}, {orderBy}";

        return context.Set<T>().FromSqlRaw(query);
    }

    /// <summary>
    /// Executes aggregate window functions for running totals, moving averages, and cumulative statistics.
    /// Provides powerful analytical capabilities for trend analysis and rolling calculations.
    /// </summary>
    /// <typeparam name="T">The entity type for the query result.</typeparam>
    /// <param name="context">The DbContext to execute the query against.</param>
    /// <param name="tableName">Name of the table to query.</param>
    /// <param name="aggregateColumn">Column to perform aggregations on.</param>
    /// <param name="partitionColumns">Columns to partition the calculations by.</param>
    /// <param name="orderColumn">Column to order the window frame by.</param>
    /// <param name="windowFrame">Window frame specification (e.g., "ROWS 6 PRECEDING").</param>
    /// <param name="aggregateFunctions">Types of aggregate functions to include.</param>
    /// <param name="additionalColumns">Additional columns to include in the result.</param>
    /// <returns>A queryable result set with aggregate window calculations.</returns>
    /// <exception cref="ArgumentException">Thrown when parameters are null or whitespace.</exception>
    /// <example>
    /// <code>
    /// // Financial time series analysis with multiple aggregates
    /// var financialAnalysis = context.ExecuteAggregateWindowFunction&lt;FinancialMetrics&gt;(
    ///     tableName: "stock_prices",
    ///     aggregateColumn: "close_price",
    ///     partitionColumns: new[] { "symbol" },
    ///     orderColumn: "trade_date",
    ///     windowFrame: "ROWS 19 PRECEDING", // 20-day window
    ///     aggregateFunctions: WindowAggregateFunction.All,
    ///     additionalColumns: new[] { "symbol", "trade_date", "close_price", "volume" });
    /// 
    /// // Generated SQL:
    /// // SELECT 
    /// //   symbol, trade_date, close_price, volume,
    /// //   SUM(close_price) OVER (PARTITION BY symbol ORDER BY trade_date ROWS 19 PRECEDING) as running_sum,
    /// //   AVG(close_price) OVER (PARTITION BY symbol ORDER BY trade_date ROWS 19 PRECEDING) as moving_avg_20d,
    /// //   COUNT(close_price) OVER (PARTITION BY symbol ORDER BY trade_date ROWS 19 PRECEDING) as sample_count,
    /// //   MIN(close_price) OVER (PARTITION BY symbol ORDER BY trade_date ROWS 19 PRECEDING) as min_20d,
    /// //   MAX(close_price) OVER (PARTITION BY symbol ORDER BY trade_date ROWS 19 PRECEDING) as max_20d,
    /// //   STDDEV(close_price) OVER (PARTITION BY symbol ORDER BY trade_date ROWS 19 PRECEDING) as volatility_20d
    /// // FROM stock_prices
    /// // ORDER BY symbol, trade_date;
    /// 
    /// // Enables financial analysis like:
    /// // - 20-day moving averages
    /// // - Rolling volatility calculations
    /// // - Support/resistance levels (min/max)
    /// // - Volume-weighted indicators
    /// </code>
    /// </example>
    public static IQueryable<T> ExecuteAggregateWindowFunction<T>(
        this DbContext context,
        string tableName,
        string aggregateColumn,
        string[] partitionColumns,
        string orderColumn,
        string windowFrame = "ROWS UNBOUNDED PRECEDING",
        WindowAggregateFunction aggregateFunctions = WindowAggregateFunction.Sum | WindowAggregateFunction.Average,
        string[]? additionalColumns = null) where T : class
    {
        if (string.IsNullOrWhiteSpace(tableName))
            throw new ArgumentException("Table name cannot be null or whitespace.", nameof(tableName));
        if (string.IsNullOrWhiteSpace(aggregateColumn))
            throw new ArgumentException("Aggregate column cannot be null or whitespace.", nameof(aggregateColumn));
        if (partitionColumns == null || partitionColumns.Length == 0)
            throw new ArgumentException("Partition columns cannot be null or empty.", nameof(partitionColumns));
        if (string.IsNullOrWhiteSpace(orderColumn))
            throw new ArgumentException("Order column cannot be null or whitespace.", nameof(orderColumn));

        var partitionBy = string.Join(", ", partitionColumns);
        var windowClause = $"PARTITION BY {partitionBy} ORDER BY {orderColumn} {windowFrame}";

        var selectColumns = new List<string>();
        if (additionalColumns != null && additionalColumns.Length > 0)
        {
            selectColumns.AddRange(additionalColumns);
        }

        var aggregateFunctionList = new List<string>();

        if (aggregateFunctions.HasFlag(WindowAggregateFunction.Sum))
            aggregateFunctionList.Add($"SUM({aggregateColumn}) OVER ({windowClause}) as running_sum");
        if (aggregateFunctions.HasFlag(WindowAggregateFunction.Average))
            aggregateFunctionList.Add($"AVG({aggregateColumn}) OVER ({windowClause}) as moving_avg");
        if (aggregateFunctions.HasFlag(WindowAggregateFunction.Count))
            aggregateFunctionList.Add($"COUNT({aggregateColumn}) OVER ({windowClause}) as sample_count");
        if (aggregateFunctions.HasFlag(WindowAggregateFunction.Min))
            aggregateFunctionList.Add($"MIN({aggregateColumn}) OVER ({windowClause}) as window_min");
        if (aggregateFunctions.HasFlag(WindowAggregateFunction.Max))
            aggregateFunctionList.Add($"MAX({aggregateColumn}) OVER ({windowClause}) as window_max");
        if (aggregateFunctions.HasFlag(WindowAggregateFunction.StandardDeviation))
            aggregateFunctionList.Add($"STDDEV({aggregateColumn}) OVER ({windowClause}) as std_deviation");

        var allColumns = string.Join(", ", selectColumns.Concat(aggregateFunctionList));
        var query = $@"
            SELECT {allColumns}
            FROM {tableName}
            ORDER BY {partitionBy}, {orderColumn}";

        return context.Set<T>().FromSqlRaw(query);
    }

    /// <summary>
    /// Executes lag/lead window functions for period-over-period analysis and time series comparisons.
    /// Provides access to values from previous or subsequent rows for trend analysis.
    /// </summary>
    /// <typeparam name="T">The entity type for the query result.</typeparam>
    /// <param name="context">The DbContext to execute the query against.</param>
    /// <param name="tableName">Name of the table to query.</param>
    /// <param name="valueColumn">Column to retrieve lag/lead values from.</param>
    /// <param name="partitionColumns">Columns to partition the lag/lead calculations by.</param>
    /// <param name="orderColumn">Column to order the lag/lead calculations by.</param>
    /// <param name="lagOffset">Number of rows to look back (lag).</param>
    /// <param name="leadOffset">Number of rows to look ahead (lead).</param>
    /// <param name="defaultValue">Default value when lag/lead is not available.</param>
    /// <param name="additionalColumns">Additional columns to include in the result.</param>
    /// <returns>A queryable result set with lag/lead calculations.</returns>
    /// <exception cref="ArgumentException">Thrown when parameters are null or whitespace.</exception>
    /// <example>
    /// <code>
    /// // Monthly sales comparison with previous and next months
    /// var salesComparison = context.ExecuteLagLeadFunction&lt;SalesComparison&gt;(
    ///     tableName: "monthly_sales",
    ///     valueColumn: "total_sales",
    ///     partitionColumns: new[] { "product_category" },
    ///     orderColumn: "sales_month",
    ///     lagOffset: 1,    // Previous month
    ///     leadOffset: 1,   // Next month
    ///     defaultValue: "0",
    ///     additionalColumns: new[] { "product_category", "sales_month", "total_sales" });
    /// 
    /// // Generated SQL:
    /// // SELECT 
    /// //   product_category, sales_month, total_sales,
    /// //   LAG(total_sales, 1, 0) OVER (PARTITION BY product_category ORDER BY sales_month) as previous_month_sales,
    /// //   LEAD(total_sales, 1, 0) OVER (PARTITION BY product_category ORDER BY sales_month) as next_month_sales,
    /// //   -- Calculated differences
    /// //   total_sales - LAG(total_sales, 1, 0) OVER (PARTITION BY product_category ORDER BY sales_month) as month_over_month_change,
    /// //   CASE WHEN LAG(total_sales, 1, 0) OVER (PARTITION BY product_category ORDER BY sales_month) > 0
    /// //        THEN ((total_sales - LAG(total_sales, 1, 0) OVER (PARTITION BY product_category ORDER BY sales_month)) / 
    /// //              LAG(total_sales, 1, 0) OVER (PARTITION BY product_category ORDER BY sales_month)) * 100
    /// //        ELSE NULL END as month_over_month_pct_change
    /// // FROM monthly_sales
    /// // ORDER BY product_category, sales_month;
    /// 
    /// // Enables analysis like:
    /// // - Month-over-month growth rates
    /// // - Seasonal trend identification
    /// // - Forward-looking comparisons
    /// // - Data continuity validation
    /// </code>
    /// </example>
    public static IQueryable<T> ExecuteLagLeadFunction<T>(
        this DbContext context,
        string tableName,
        string valueColumn,
        string[] partitionColumns,
        string orderColumn,
        int lagOffset = 1,
        int leadOffset = 1,
        string defaultValue = "NULL",
        string[]? additionalColumns = null) where T : class
    {
        if (string.IsNullOrWhiteSpace(tableName))
            throw new ArgumentException("Table name cannot be null or whitespace.", nameof(tableName));
        if (string.IsNullOrWhiteSpace(valueColumn))
            throw new ArgumentException("Value column cannot be null or whitespace.", nameof(valueColumn));
        if (partitionColumns == null || partitionColumns.Length == 0)
            throw new ArgumentException("Partition columns cannot be null or empty.", nameof(partitionColumns));
        if (string.IsNullOrWhiteSpace(orderColumn))
            throw new ArgumentException("Order column cannot be null or whitespace.", nameof(orderColumn));

        var partitionBy = string.Join(", ", partitionColumns);
        var windowClause = $"PARTITION BY {partitionBy} ORDER BY {orderColumn}";

        var selectColumns = new List<string>();
        if (additionalColumns != null && additionalColumns.Length > 0)
        {
            selectColumns.AddRange(additionalColumns);
        }

        var lagLeadFunctions = new List<string>
        {
            $"LAG({valueColumn}, {lagOffset}, {defaultValue}) OVER ({windowClause}) as lag_{lagOffset}_value",
            $"LEAD({valueColumn}, {leadOffset}, {defaultValue}) OVER ({windowClause}) as lead_{leadOffset}_value",
            // Calculate differences
            $"{valueColumn} - LAG({valueColumn}, {lagOffset}, {defaultValue}) OVER ({windowClause}) as lag_{lagOffset}_diff",
            // Calculate percentage changes
            $@"CASE WHEN LAG({valueColumn}, {lagOffset}, {defaultValue}) OVER ({windowClause}) > 0
                    THEN (({valueColumn} - LAG({valueColumn}, {lagOffset}, {defaultValue}) OVER ({windowClause})) / 
                          LAG({valueColumn}, {lagOffset}, {defaultValue}) OVER ({windowClause})) * 100
                    ELSE NULL END as lag_{lagOffset}_pct_change"
        };

        var allColumns = string.Join(", ", selectColumns.Concat(lagLeadFunctions));
        var query = $@"
            SELECT {allColumns}
            FROM {tableName}
            ORDER BY {partitionBy}, {orderColumn}";

        return context.Set<T>().FromSqlRaw(query);
    }

    #endregion
}

/// <summary>
/// Represents the available refresh strategies for materialized views.
/// </summary>
public enum MaterializedViewRefreshStrategy
{
    /// <summary>Manual refresh only.</summary>
    Manual,
    
    /// <summary>Refresh every hour.</summary>
    Hourly,
    
    /// <summary>Refresh once per day.</summary>
    Daily,
    
    /// <summary>Refresh once per week.</summary>
    Weekly,
    
    /// <summary>Refresh once per month.</summary>
    Monthly,
    
    /// <summary>Custom refresh strategy (requires manual configuration).</summary>
    Custom
}

/// <summary>
/// Represents the types of materialized view refresh operations.
/// </summary>
public enum MaterializedViewRefreshType
{
    /// <summary>Complete refresh of all data.</summary>
    Complete,
    
    /// <summary>Incremental refresh (if supported by the view).</summary>
    Incremental
}

/// <summary>
/// Represents the available window aggregate functions.
/// </summary>
[Flags]
public enum WindowAggregateFunction
{
    /// <summary>Sum aggregation.</summary>
    Sum = 1,
    
    /// <summary>Average aggregation.</summary>
    Average = 2,
    
    /// <summary>Count aggregation.</summary>
    Count = 4,
    
    /// <summary>Minimum aggregation.</summary>
    Min = 8,
    
    /// <summary>Maximum aggregation.</summary>
    Max = 16,
    
    /// <summary>Standard deviation aggregation.</summary>
    StandardDeviation = 32,
    
    /// <summary>All available aggregate functions.</summary>
    All = Sum | Average | Count | Min | Max | StandardDeviation
}