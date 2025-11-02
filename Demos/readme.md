# Transform Chaos into Order with Azure Durable Functions

## Overview

Azure Durable Functions extend Azure Functions with stateful capabilities, enabling you to orchestrate complex workflows in serverless environments. By abstracting away state management, checkpointing, and restarts, Durable Functions transform the chaos of distributed computing into ordered, reliable workflows.

This repository contains hands-on labs demonstrating four essential Durable Functions application patterns, each addressing different challenges in distributed systems:

- **Async HTTP API**: Transform long-running operations into manageable async workflows
- **Function Chaining**: Create order from complex sequential processes  
- **Fan-Out/Fan-In**: Orchestrate parallel tasks with coordinated results
- **Monitor**: Maintain a persistent state for long-running monitoring scenarios

## 🚀 Quick Start

### Completed Solutions
Browse the working implementations: [`src/`](./src/)

### Hands-On Labs
Build each pattern yourself with step-by-step guidance:

1. [**Initialize Your Solution**](./00-initialize-solution.md) - Set up your development environment.
2. [**Async HTTP API Pattern**](./01-async-http-api.md) - Handle long-running operations elegantly.
3. [**Function Chaining Pattern**](./02-function-chaining.md) - Orchestrate sequential workflows.
4. [**Fan-Out/Fan-In Pattern**](./03-fan-out-fan-in.md) - Process tasks in parallel.
5. [**Monitor Pattern**](./04-monitor.md) - Implement stateful monitoring logic.

## Prerequisites

- [Azure Functions Core Tools v4.x](https://github.com/Azure/azure-functions-core-tools/releases)
- [.NET 8.0 or later](https://dotnet.microsoft.com/en-us/download/dotnet)
- [Download Visual Studio Code - Mac, Linux, Windows](https://code.visualstudio.com/download) or [Visual Studio 2022](https://visualstudio.microsoft.com/)
- [Azure Storage Emulator](https://learn.microsoft.com/en-us/azure/storage/common/storage-use-emulator) or [Azurite](https://learn.microsoft.com/en-us/azure/storage/common/storage-install-azurite?toc=%2Fazure%2Fstorage%2Fblobs%2Ftoc.json&bc=%2Fazure%2Fstorage%2Fblobs%2Fbreadcrumb%2Ftoc.json&tabs=visual-studio%2Cblob-storage)
- Basic familiarity with C# and Azure Functions

## Application Patterns Deep Dive

### 🔄 Async HTTP API
**Challenge**: HTTP timeouts limit long-running operations  
**Solution**: Return status endpoints for polling operation progress

### 🔗 Function Chaining  
**Challenge**: Complex workflows with dependent steps become unmanageable  
**Solution**: Orchestrate sequential activities with automatic state management

### 🌐 Fan-Out/Fan-In
**Challenge**: Coordinating parallel processing with result aggregation  
**Solution**: Dynamically spawn parallel tasks and await collective completion

### 📊 Monitor
**Challenge**: Implementing recurring checks with state persistence  
**Solution**: Durable timers and state for long-running monitoring scenarios

## Getting Started

Each lab builds upon core concepts while introducing new patterns. Start with the [initialization guide](./00-initialize-solution.md) to set up your environment, then progress through the patterns in order. Each lab includes:

- Pattern overview and use cases
- Step-by-step implementation
- Testing guidance
- Challenge extensions

Ready to transform chaos into order? [**Start with Lab 00 →**](./00-initialize-solution.md)
