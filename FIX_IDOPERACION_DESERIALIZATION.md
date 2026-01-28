# Fix: IdOperacion Not Reaching AddCargoButton Dialog

## Problem Statement

The "Agregar Cargo" button in the Cargos pivot of OperacionesView was not working correctly. When clicked, the button handler would abort at the validation:

```csharp
if (!operacion.IdOperacion.HasValue) return;
```

This prevented the dialog from opening. When this validation was temporarily commented out and the ID was forced, the dialog worked perfectly, confirming that the only issue was that `idOperacion` was not arriving at the handler.

## Root Cause Analysis

After thorough investigation, the issue was identified as a **JSON deserialization case sensitivity mismatch**:

### The Problem Chain:

1. **API Response**: The API returns properties in **PascalCase** format
   ```json
   {
     "IdOperacion": 123,
     "IdTipo": 1,
     "RazonSocial": "Cliente ABC",
     ...
   }
   ```

2. **Model Definition**: The `OperacionDto` model uses `JsonPropertyName` attributes in **camelCase**:
   ```csharp
   [JsonPropertyName("idOperacion")]
   public int? IdOperacion { get; set; }
   ```

3. **Deserialization**: The services were using `ReadFromJsonAsync` without custom options:
   ```csharp
   var operaciones = await response.Content
       .ReadFromJsonAsync<List<OperacionDto>>(cancellationToken: cancellationToken);
   ```

4. **Default Behavior**: System.Text.Json is **case-sensitive by default**, so:
   - API property `"IdOperacion"` (PascalCase) 
   - Did NOT match `[JsonPropertyName("idOperacion")]` (camelCase)
   - Result: Property remained `null` after deserialization

5. **Button Handler**: The validation `if (!operacion.IdOperacion.HasValue)` would return true, aborting the method

## Solution

Added case-insensitive JSON deserialization to the affected services:

### Changes to OperacionService.cs:

```csharp
public class OperacionService : IOperacionService
{
    private readonly HttpClient _http;
    private readonly IApiEndpointProvider _endpoints;
    private readonly ILoggingService _logger;
    private readonly JsonSerializerOptions _jsonOptions;  // ← NEW

    public OperacionService(HttpClient http, IApiEndpointProvider endpoints, ILoggingService logger)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // ← NEW: Configure case-insensitive deserialization
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<List<OperacionDto>> GetOperacionesAsync(...)
    {
        // ...
        
        // ← UPDATED: Pass options to ReadFromJsonAsync
        var operaciones = await response.Content
            .ReadFromJsonAsync<List<OperacionDto>>(_jsonOptions, cancellationToken: cancellationToken);
        
        // ...
    }
}
```

### Changes to CargoService.cs:

Applied the same pattern for consistency, ensuring cargos are also deserialized correctly.

## Why This Was NOT a Binding Issue

Initial investigation focused on the XAML binding (`Tag="{x:Bind}"`), but the binding was working correctly:

1. ✅ The button's `Tag` was properly set to `{x:Bind}`
2. ✅ The DataTemplate's `x:DataType="local:OperacionDto"` was correct
3. ✅ The button click handler was being called
4. ✅ The `OperacionDto` object was being passed to the handler
5. ❌ BUT the `IdOperacion` property within that object was `null`

The issue was purely about data deserialization, not UI binding.

## Impact

### Before Fix:
- User clicks "Agregar Cargo" button
- Handler checks `if (!operacion.IdOperacion.HasValue)`
- Condition is true (IdOperacion is null)
- Method returns early
- Dialog never appears ❌

### After Fix:
- User clicks "Agregar Cargo" button
- Handler checks `if (!operacion.IdOperacion.HasValue)`
- Condition is false (IdOperacion has value from API)
- Dialog appears with correct operation ID ✅

## Files Modified

```
Advance Control/
└── Services/
    ├── Operaciones/
    │   └── OperacionService.cs      (Added case-insensitive JSON options)
    └── Cargos/
        └── CargoService.cs          (Added case-insensitive JSON options)
```

## Testing Recommendations

To verify the fix:

1. **Launch the application** on a Windows machine
2. **Navigate to Operaciones page**
3. **Click on an operation** to expand it
4. **Click the "Cargos" tab**
5. **Click "Agregar Cargo" button**
6. **Expected result**: Dialog appears showing:
   - Title: "Agregar Cargo"
   - Content: "ID de la Operación: [actual_id]"
   - Buttons: "Agregar" and "Cancelar"

## Technical Notes

### Why PropertyNameCaseInsensitive?

The `PropertyNameCaseInsensitive` option tells System.Text.Json to:
- Ignore case when matching JSON property names to .NET property names
- Match "IdOperacion", "idOperacion", "IDOPERACION", etc. to the same property
- Provide flexibility when API and client use different casing conventions

### Alternative Solutions (Not Used)

1. **Change JsonPropertyName to match API**: Would require updating all models
2. **Global JSON configuration**: Would require changes to HttpClient setup in App.xaml.cs
3. **Fix the API**: Outside scope of client application

The chosen solution is minimal, localized, and doesn't require changes to other services or the API.

## Security Considerations

✅ **Code Review**: Completed - No issues found  
✅ **Security Scan**: Completed - No vulnerabilities detected  
✅ **Impact**: Low risk - Only changes JSON deserialization behavior

## Related Documentation

- Previous fix: `VISUAL_SUMMARY_AGREGAR_CARGO.md` - Addressed XAML binding (from `{Binding}` to `{x:Bind}`)
- This fix: Addresses data deserialization (case sensitivity)

---

**Date**: 2026-01-28  
**Status**: ✅ Completed  
**Validation**: Code review and security scan passed
