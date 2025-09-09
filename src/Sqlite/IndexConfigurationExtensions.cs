namespace Wangkanai.EntityFramework.Sqlite;

using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

/// <summary>
/// Extension methods for advanced SQLite indexing configurations.
/// Provides SQLite-specific optimizations for index creation and management.
/// </summary>
public static class IndexConfigurationExtensions
{
    /// <summary>
    /// Creates a partial index with a WHERE condition for filtered queries.
    /// Partial indexes are smaller and faster for queries that match the condition.
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <param name="indexBuilder">The index builder</param>
    /// <param name="filter">The filter condition for the partial index</param>
    /// <returns>The index builder for method chaining</returns>
    /// <exception cref="ArgumentNullException">Thrown when indexBuilder or filter is null</exception>
    /// <example>
    /// <code>
    /// entity.HasIndex(e => e.Status)
    ///       .HasSqlitePartialIndex&lt;Order&gt;(o => o.IsActive);
    /// </code>
    /// </example>
    public static IndexBuilder<T> HasSqlitePartialIndex<T>(
        this IndexBuilder<T> indexBuilder,
        Expression<Func<T, bool>> filter) where T : class
    {
        ArgumentNullException.ThrowIfNull(indexBuilder);
        ArgumentNullException.ThrowIfNull(filter);

        // Convert the expression to a SQL WHERE clause
        var filterSql = ConvertFilterExpressionToSql(filter);
        
        return indexBuilder.HasFilter(filterSql);
    }

    /// <summary>
    /// Creates a covering index that includes additional columns to eliminate table lookups.
    /// The included columns are stored at the leaf level of the index for faster access.
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <param name="indexBuilder">The index builder</param>
    /// <param name="includeProperties">Properties to include in the covering index</param>
    /// <returns>The index builder for method chaining</returns>
    /// <exception cref="ArgumentNullException">Thrown when indexBuilder or includeProperties is null</exception>
    /// <exception cref="ArgumentException">Thrown when includeProperties is empty</exception>
    /// <example>
    /// <code>
    /// entity.HasIndex(e => e.CustomerId)
    ///       .HasSqliteCoveringIndex&lt;Order&gt;(o => o.OrderDate, o => o.TotalAmount);
    /// </code>
    /// </example>
    public static IndexBuilder<T> HasSqliteCoveringIndex<T>(
        this IndexBuilder<T> indexBuilder,
        params Expression<Func<T, object>>[] includeProperties) where T : class
    {
        ArgumentNullException.ThrowIfNull(indexBuilder);
        ArgumentNullException.ThrowIfNull(includeProperties);
        
        if (includeProperties.Length == 0)
            throw new ArgumentException("At least one property must be specified for covering index", nameof(includeProperties));

        // Extract property names from expressions
        var propertyNames = includeProperties
            .Select(ExtractPropertyName)
            .Where(name => !string.IsNullOrEmpty(name))
            .ToArray();

        if (propertyNames.Length == 0)
            throw new ArgumentException("No valid properties found in includeProperties", nameof(includeProperties));

        return indexBuilder.IncludeProperties(propertyNames);
    }

    /// <summary>
    /// Creates an expression-based index for computed values or function calls.
    /// Useful for indexing calculated fields, LOWER() expressions, or substring operations.
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <param name="entityBuilder">The entity type builder</param>
    /// <param name="indexExpression">The computed expression to index</param>
    /// <param name="indexName">Optional custom name for the index</param>
    /// <returns>The index builder for method chaining</returns>
    /// <exception cref="ArgumentNullException">Thrown when entityBuilder or indexExpression is null</exception>
    /// <example>
    /// <code>
    /// entity.HasSqliteExpressionIndex&lt;Customer&gt;(
    ///     c => EF.Functions.Lower(c.Email),
    ///     "IX_Customer_Email_Lower");
    /// </code>
    /// </example>
    public static IndexBuilder HasSqliteExpressionIndex<T>(
        this EntityTypeBuilder<T> entityBuilder,
        Expression<Func<T, object>> indexExpression,
        string? indexName = null) where T : class
    {
        ArgumentNullException.ThrowIfNull(entityBuilder);
        ArgumentNullException.ThrowIfNull(indexExpression);

        // Generate index name if not provided
        indexName ??= GenerateExpressionIndexName<T>(indexExpression);
        
        // Convert expression to SQL for SQLite
        var sqlExpression = ConvertExpressionToSql(indexExpression);
        
        return entityBuilder.HasIndex(indexExpression, indexName)
                          .HasDatabaseName(indexName)
                          .HasAnnotation("Relational:SqlExpression", sqlExpression);
    }

    /// <summary>
    /// Optimizes indexes specifically for range queries and sorting operations.
    /// Configures the index with appropriate collation and sort order for efficient range scans.
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <param name="indexBuilder">The index builder</param>
    /// <param name="ascending">Whether to sort in ascending order (default: true)</param>
    /// <param name="collation">Optional collation for text columns</param>
    /// <returns>The index builder for method chaining</returns>
    /// <exception cref="ArgumentNullException">Thrown when indexBuilder is null</exception>
    /// <example>
    /// <code>
    /// entity.HasIndex(e => e.OrderDate)
    ///       .OptimizeForSqliteRangeQueries&lt;Order&gt;(ascending: false);
    /// </code>
    /// </example>
    public static IndexBuilder<T> OptimizeForSqliteRangeQueries<T>(
        this IndexBuilder<T> indexBuilder,
        bool ascending = true,
        string? collation = null) where T : class
    {
        ArgumentNullException.ThrowIfNull(indexBuilder);

        // Set sort order for range query optimization
        indexBuilder = ascending ? indexBuilder.IsDescending(false) : indexBuilder.IsDescending(true);

        // Apply collation if specified
        if (!string.IsNullOrEmpty(collation))
        {
            indexBuilder.HasAnnotation("Relational:Collation", collation);
        }

        // Add SQLite-specific optimization hints
        indexBuilder.HasAnnotation("Sqlite:OptimizedForRangeQueries", true);
        
        // Configure for efficient range scans
        return indexBuilder.HasAnnotation("Sqlite:IndexType", "RANGE_OPTIMIZED");
    }

    #region Private Helper Methods

    /// <summary>
    /// Converts a filter expression to SQL WHERE clause syntax for SQLite.
    /// </summary>
    private static string ConvertFilterExpressionToSql<T>(Expression<Func<T, bool>> filter)
    {
        // This is a simplified implementation. In a production environment,
        // you would use a more sophisticated expression visitor to convert
        // the LINQ expression to proper SQL syntax.
        
        var body = filter.Body;
        return body switch
        {
            BinaryExpression binaryExpr when binaryExpr.NodeType == ExpressionType.Equal =>
                ConvertBinaryExpression(binaryExpr, "="),
            BinaryExpression binaryExpr when binaryExpr.NodeType == ExpressionType.NotEqual =>
                ConvertBinaryExpression(binaryExpr, "!="),
            BinaryExpression binaryExpr when binaryExpr.NodeType == ExpressionType.GreaterThan =>
                ConvertBinaryExpression(binaryExpr, ">"),
            BinaryExpression binaryExpr when binaryExpr.NodeType == ExpressionType.GreaterThanOrEqual =>
                ConvertBinaryExpression(binaryExpr, ">="),
            BinaryExpression binaryExpr when binaryExpr.NodeType == ExpressionType.LessThan =>
                ConvertBinaryExpression(binaryExpr, "<"),
            BinaryExpression binaryExpr when binaryExpr.NodeType == ExpressionType.LessThanOrEqual =>
                ConvertBinaryExpression(binaryExpr, "<="),
            MemberExpression memberExpr when memberExpr.Type == typeof(bool) =>
                $"[{memberExpr.Member.Name}] = 1",
            UnaryExpression unaryExpr when unaryExpr.NodeType == ExpressionType.Not &&
                                         unaryExpr.Operand is MemberExpression memberOperand &&
                                         memberOperand.Type == typeof(bool) =>
                $"[{memberOperand.Member.Name}] = 0",
            _ => throw new NotSupportedException($"Expression type {body.NodeType} is not supported for partial indexes")
        };
    }

    /// <summary>
    /// Converts a binary expression to SQL syntax.
    /// </summary>
    private static string ConvertBinaryExpression(BinaryExpression binaryExpr, string sqlOperator)
    {
        var leftMember = ExtractMemberName(binaryExpr.Left);
        var rightValue = ExtractConstantValue(binaryExpr.Right);
        
        return $"[{leftMember}] {sqlOperator} {rightValue}";
    }

    /// <summary>
    /// Extracts the member name from an expression.
    /// </summary>
    private static string ExtractMemberName(Expression expression)
    {
        return expression switch
        {
            MemberExpression memberExpr => memberExpr.Member.Name,
            UnaryExpression { Operand: MemberExpression memberExpr } => memberExpr.Member.Name,
            _ => throw new NotSupportedException($"Cannot extract member name from expression type {expression.NodeType}")
        };
    }

    /// <summary>
    /// Extracts a constant value from an expression and formats it for SQL.
    /// </summary>
    private static string ExtractConstantValue(Expression expression)
    {
        var value = expression switch
        {
            ConstantExpression constantExpr => constantExpr.Value,
            MemberExpression memberExpr when memberExpr.Expression is ConstantExpression constantParent =>
                GetMemberValue(memberExpr, constantParent),
            _ => throw new NotSupportedException($"Cannot extract constant value from expression type {expression.NodeType}")
        };

        return value switch
        {
            string str => $"'{str.Replace("'", "''")}'", // Escape single quotes
            bool b => b ? "1" : "0",
            null => "NULL",
            _ => value.ToString() ?? "NULL"
        };
    }

    /// <summary>
    /// Gets the value from a member expression with a constant parent.
    /// </summary>
    private static object? GetMemberValue(MemberExpression memberExpr, ConstantExpression constantParent)
    {
        var parentValue = constantParent.Value;
        return memberExpr.Member switch
        {
            System.Reflection.FieldInfo field => field.GetValue(parentValue),
            System.Reflection.PropertyInfo property => property.GetValue(parentValue),
            _ => throw new NotSupportedException($"Member type {memberExpr.Member.GetType().Name} is not supported")
        };
    }

    /// <summary>
    /// Extracts the property name from a lambda expression.
    /// </summary>
    private static string ExtractPropertyName<T>(Expression<Func<T, object>> propertyExpression)
    {
        return propertyExpression.Body switch
        {
            MemberExpression memberExpr => memberExpr.Member.Name,
            UnaryExpression { Operand: MemberExpression memberExpr } => memberExpr.Member.Name,
            _ => string.Empty
        };
    }

    /// <summary>
    /// Generates a unique index name for expression-based indexes.
    /// </summary>
    private static string GenerateExpressionIndexName<T>(Expression<Func<T, object>> expression)
    {
        var entityName = typeof(T).Name;
        var expressionHash = expression.ToString().GetHashCode().ToString("X8");
        return $"IX_{entityName}_Expression_{expressionHash}";
    }

    /// <summary>
    /// Converts an expression to SQL syntax for SQLite expression indexes.
    /// </summary>
    private static string ConvertExpressionToSql<T>(Expression<Func<T, object>> expression)
    {
        // This is a simplified implementation. In production, you would use
        // a more comprehensive expression visitor to handle complex expressions.
        
        return expression.Body switch
        {
            MethodCallExpression methodCall when methodCall.Method.Name == "Lower" =>
                $"LOWER([{ExtractPropertyName(expression)}])",
            MethodCallExpression methodCall when methodCall.Method.Name == "Upper" =>
                $"UPPER([{ExtractPropertyName(expression)}])",
            MethodCallExpression methodCall when methodCall.Method.Name == "Substring" =>
                ConvertSubstringExpression(methodCall),
            MemberExpression => $"[{ExtractPropertyName(expression)}]",
            _ => throw new NotSupportedException($"Expression type {expression.Body.NodeType} is not supported for expression indexes")
        };
    }

    /// <summary>
    /// Converts a substring method call to SQL SUBSTR function.
    /// </summary>
    private static string ConvertSubstringExpression(MethodCallExpression methodCall)
    {
        if (methodCall.Object is not MemberExpression memberExpr)
            throw new NotSupportedException("Substring must be called on a property");

        var propertyName = memberExpr.Member.Name;
        var arguments = methodCall.Arguments;

        return arguments.Count switch
        {
            1 => $"SUBSTR([{propertyName}], {ExtractConstantValue(arguments[0])} + 1)", // SQLite SUBSTR is 1-based
            2 => $"SUBSTR([{propertyName}], {ExtractConstantValue(arguments[0])} + 1, {ExtractConstantValue(arguments[1])})",
            _ => throw new NotSupportedException("Substring with more than 2 arguments is not supported")
        };
    }

    #endregion
}