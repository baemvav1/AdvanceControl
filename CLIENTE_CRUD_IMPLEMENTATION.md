# Cliente CRUD Operations Implementation

## Overview

This document describes the implementation of CRUD (Create, Read, Update, Delete) operations for the Cliente (Customer) service in the Advance Control WinUI application.

## Changes Made

### 1. New DTO Model

**File**: `Advance Control/Models/ClienteEditDto.cs`

A new Data Transfer Object (DTO) was created to support the updated API endpoint that uses the `sp_cliente_edit` stored procedure. This DTO includes:

- **Operacion**: Operation type ('select', 'create', 'update', 'delete')
- **IdCliente**: Customer ID (required for update and delete operations)
- **Rfc**: Customer RFC (tax identification)
- **RazonSocial**: Legal business name
- **NombreComercial**: Commercial name
- **RegimenFiscal**: Tax regime
- **UsoCfdi**: CFDI usage
- **DiasCredito**: Credit days
- **LimiteCredito**: Credit limit
- **Prioridad**: Customer priority
- **Estatus**: Status (active/inactive)
- **CredencialId**: Associated credential ID
- **Notas**: Additional notes
- **IdUsuario**: User ID performing the operation

### 2. Interface Updates

**File**: `Advance Control/Services/Clientes/IClienteService.cs`

Three new methods were added to the interface:

```csharp
Task<object> CreateClienteAsync(ClienteEditDto query, CancellationToken cancellationToken = default);
Task<object> UpdateClienteAsync(ClienteEditDto query, CancellationToken cancellationToken = default);
Task<object> DeleteClienteAsync(int idCliente, int? idUsuario, CancellationToken cancellationToken = default);
```

### 3. Service Implementation

**File**: `Advance Control/Services/Clientes/ClienteService.cs`

#### CreateClienteAsync
- **HTTP Method**: POST
- **Endpoint**: `/api/Clientes`
- **Purpose**: Creates a new customer
- **Returns**: Object with operation result (success/failure with message)

#### UpdateClienteAsync
- **HTTP Method**: PUT
- **Endpoint**: `/api/Clientes/{idCliente}`
- **Purpose**: Updates an existing customer by ID
- **Returns**: Object with operation result (success/failure with message)

#### DeleteClienteAsync
- **HTTP Method**: DELETE
- **Endpoint**: `/api/Clientes/{idCliente}?idUsuario={idUsuario}`
- **Purpose**: Performs soft delete on a customer
- **Returns**: Object with operation result (success/failure with message)

## Implementation Details

All methods follow the established patterns:

1. **Error Handling**: Each method includes comprehensive error handling with try-catch blocks
2. **Logging**: All operations are logged using the ILoggingService
3. **CancellationToken Support**: All methods support cancellation tokens for async operations
4. **ConfigureAwait(false)**: Used consistently to avoid deadlocks
5. **HTTP Status Code Checking**: All methods check response status codes and handle errors appropriately
6. **Response Deserialization**: Responses are properly deserialized from JSON

## API Compatibility

The implementation matches the server-side API that uses:
- Stored procedure: `sp_cliente_edit`
- Operations: select, create, update, delete
- RESTful HTTP methods: GET, POST, PUT, DELETE

## Usage Examples

### Creating a Customer

```csharp
var clienteDto = new ClienteEditDto
{
    Rfc = "ABC123456DEF",
    RazonSocial = "Empresa XYZ S.A. de C.V.",
    NombreComercial = "Empresa XYZ",
    RegimenFiscal = "601",
    UsoCfdi = "G03",
    DiasCredito = 30,
    LimiteCredito = 100000.00M,
    Prioridad = 1,
    IdUsuario = currentUserId
};

var result = await clienteService.CreateClienteAsync(clienteDto);
```

### Updating a Customer

```csharp
var clienteDto = new ClienteEditDto
{
    IdCliente = 123,
    RazonSocial = "Empresa XYZ S.A. de C.V. - Actualizada",
    Estatus = true,
    IdUsuario = currentUserId
    // ... other fields
};

var result = await clienteService.UpdateClienteAsync(clienteDto);
```

### Deleting a Customer

```csharp
var result = await clienteService.DeleteClienteAsync(
    idCliente: 123, 
    idUsuario: currentUserId
);
```

## Testing

Since this is a Windows-only WinUI application that cannot be built on Linux systems, the implementation:

1. Follows the exact same patterns as existing service methods
2. Uses the same error handling, logging, and response processing
3. Is syntactically correct and consistent with the codebase style
4. Will be tested when the application is built and run on a Windows system

## Next Steps

To fully integrate these new CRUD operations:

1. Update the CustomersViewModel to use the new methods
2. Add UI components for creating, editing, and deleting customers
3. Add appropriate authorization checks
4. Consider adding unit tests for the new methods (when testing infrastructure is available)
5. Update user documentation to reflect new capabilities

## Notes

- The existing `GetClientesAsync` method remains unchanged and continues to work with `ClienteQueryDto`
- All new methods are backward compatible
- The implementation maintains consistency with other services in the application (e.g., MantenimientoService, OperacionService)
