// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

namespace Wangkanai.Foundation;

/// <summary>
/// Represents a factory interface that serves as a blueprint for creating objects or
/// instances without specifying the exact class of the object that will be created.
/// This pattern provides a way to encapsulate the creation logic and promote consistency in instantiation.
/// </summary>
public interface IFactory;

/// <summary>
/// Defines an asynchronous factory interface responsible for creating objects or
/// instances without dictating the specific class of the object being instantiated.
/// It promotes abstraction and enables asynchronous instantiation processes.
/// </summary>
public interface IAsyncFactory;