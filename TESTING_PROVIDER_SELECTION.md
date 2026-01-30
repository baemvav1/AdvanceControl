# Testing Guide: Provider Selection Feature for Refacciones

## Overview
This document describes how to test the new provider selection feature that was added to the "Agregar Cargo" dialog for refacciones. This feature allows users to select a provider when adding a refaccion charge, and the selected provider's ID will be saved with the cargo.

## Prerequisites
- Windows environment with WinUI 3 support
- .NET 8.0 SDK installed
- Access to the AdvanceControl database with test data
- At least one refaccion with associated providers in the database

## Feature Description
When a user selects a refaccion in the "Agregar Cargo" dialog:
1. The system checks if the refaccion has associated providers
2. If providers exist, a "Proveedores" button appears
3. Clicking the button displays a list of providers with their prices
4. The user can select a provider from the list
5. When the cargo is saved, the selected provider's ID is included

## Test Scenarios

### Scenario 1: Basic Provider Selection Flow

**Steps:**
1. Navigate to OperacionesView
2. Find and expand an operation to see its cargos
3. Click the "Agregar Cargo" button
4. Select "Refacción" from the "Tipo de Cargo" dropdown
5. Search for and select a refaccion that has associated providers
6. Observe the "Proveedores" button appears below the selected refaccion card
7. Click the "Proveedores" button
8. Wait for the providers list to load
9. Select a provider from the list
10. Verify the provider is highlighted/selected
11. Enter any required fields (monto should be pre-filled)
12. Click "Agregar" to create the cargo

**Expected Results:**
- The "Proveedores" button appears after selecting a refaccion with providers
- Clicking the button shows a loading spinner briefly
- A list of providers is displayed with:
  - Provider ID (in a colored badge)
  - Provider commercial name
  - Cost/Price for this refaccion
- The selected provider is highlighted
- The cargo is created successfully with the provider ID

**Visual Layout:**
```
┌─────────────────────────────────────┐
│ Agregar Cargo                       │
├─────────────────────────────────────┤
│ ID Operación: 123                   │
│                                     │
│ Tipo de Cargo: [Refacción ▼]       │
│                                     │
│ Refacción seleccionada:             │
│ ┌─────────────────────────────────┐ │
│ │ Bosch                           │ │
│ │ ABC-123                         │ │
│ │ Costo: $150.00        [Cambiar] │ │
│ └─────────────────────────────────┘ │
│                                     │
│ [Proveedores]                       │
│ ┌─────────────────────────────────┐ │
│ │ ┌─┐ Proveedor A         $150.00 │ │
│ │ │1│                             │ │← Selected
│ │ └─┘                             │ │
│ │ ┌─┐ Proveedor B         $155.00 │ │
│ │ │2│                             │ │
│ │ └─┘                             │ │
│ │ ┌─┐ Proveedor C         $148.00 │ │
│ │ │3│                             │ │
│ │ └─┘                             │ │
│ └─────────────────────────────────┘ │
│                                     │
│ Monto: [150.00]                     │
│                                     │
│ Nota (opcional): [              ]  │
├─────────────────────────────────────┤
│              [Cancelar]  [Agregar]  │
└─────────────────────────────────────┘
```

### Scenario 2: Toggle Provider List Visibility

**Steps:**
1. Follow Scenario 1 steps 1-7 to open the providers list
2. Click the "Proveedores" button again
3. Click the "Proveedores" button once more

**Expected Results:**
- First click: Providers list is shown and providers are loaded
- Second click: Providers list is hidden
- Third click: Providers list is shown again (providers don't reload)

### Scenario 3: Change Refaccion After Selecting Provider

**Steps:**
1. Follow Scenario 1 to select a refaccion and a provider
2. Click the "Cambiar" button on the selected refaccion card
3. Select a different refaccion

**Expected Results:**
- The selected refaccion card disappears
- The search fields and refaccion list reappear
- The provider selection is cleared
- When selecting a new refaccion, providers list starts fresh
- No provider is pre-selected for the new refaccion

### Scenario 4: Refaccion Without Providers

**Steps:**
1. Navigate to OperacionesView
2. Click "Agregar Cargo" button
3. Select "Refacción" from the "Tipo de Cargo" dropdown
4. Select a refaccion that has NO associated providers

**Expected Results:**
- The refaccion is selected normally
- The "Proveedores" button does NOT appear
- All other functionality works as expected
- The cargo can be created without a provider ID

### Scenario 5: Create Cargo Without Selecting Provider

**Steps:**
1. Follow Scenario 1 steps 1-6 to show the "Proveedores" button
2. Do NOT click the "Proveedores" button or select a provider
3. Fill in the required fields
4. Click "Agregar" to create the cargo

**Expected Results:**
- The cargo is created successfully
- The cargo has NO provider ID (null/empty)
- This is valid behavior - provider selection is optional

### Scenario 6: Create Cargo With Selected Provider

**Steps:**
1. Follow Scenario 1 completely to select a provider
2. Fill in any remaining required fields
3. Click "Agregar" to create the cargo

**Expected Results:**
- The cargo is created successfully
- The cargo includes the selected provider's ID
- The new cargo appears in the cargos list

### Scenario 7: Provider List Loading State

**Steps:**
1. Follow Scenario 1 steps 1-6
2. Click the "Proveedores" button
3. Observe the loading state

**Expected Results:**
- A loading spinner (ProgressRing) appears in the providers area
- The providers list is hidden during loading
- Once loaded, the spinner disappears and the list appears
- If loading is very fast, you may not see the spinner

### Scenario 8: Error Handling - Provider Fetch Failure

**Steps:**
1. Disconnect from the network or stop the API server
2. Follow Scenario 1 steps 1-7 to open the providers list
3. Observe the behavior

**Expected Results:**
- A loading spinner appears briefly
- An empty providers list is shown (no error dialog)
- An error is logged to the debug console
- The user can still create the cargo without a provider

## Database Verification

After creating cargos with the new provider selection feature, verify the data in the database:

### Query for Refaccion Cargo with Provider:
```sql
SELECT 
    c.IdCargo, 
    c.IdTipoCargo, 
    c.IdRelacionCargo, 
    c.Monto, 
    c.IdProveedor,
    r.Marca,
    r.Serie,
    p.NombreComercial as ProveedorNombre
FROM Cargos c
LEFT JOIN Refacciones r ON c.IdRelacionCargo = r.IdRefaccion AND c.IdTipoCargo = 1
LEFT JOIN Proveedores p ON c.IdProveedor = p.IdProveedor
WHERE c.IdOperacion = [test_operation_id]
AND c.IdTipoCargo = 1
ORDER BY c.IdCargo DESC;
```

**Expected for cargo with provider selected:**
- `IdTipoCargo` = 1 (Refacción)
- `IdRelacionCargo` = the refaccion ID
- `Monto` = the entered monto
- `IdProveedor` = the selected provider's ID (NOT NULL)
- `ProveedorNombre` = the provider's commercial name

**Expected for cargo without provider selected:**
- `IdTipoCargo` = 1 (Refacción)
- `IdRelacionCargo` = the refaccion ID
- `Monto` = the entered monto
- `IdProveedor` = NULL
- `ProveedorNombre` = NULL

## Integration Points

### Services Used:
1. **IRelacionProveedorRefaccionService** (new integration)
   - Method: `GetProveedoresByRefaccionAsync(int idRefaccion)`
   - Returns: `List<ProveedorPorRefaccionDto>`

2. **IRefaccionService** (existing)
   - Method: `CheckProveedorExistsAsync(int idRefaccion)`
   - Returns: `bool`

### Code Files Modified:
1. `SeleccionarRefaccionUserControl.xaml` - Added providers ListView
2. `SeleccionarRefaccionUserControl.xaml.cs` - Added provider loading and selection logic
3. `AgregarCargoUserControl.xaml.cs` - Updated to use selected provider ID

## Known Limitations and Edge Cases

1. **Optional Selection**: Provider selection is optional. Users can create cargos without selecting a provider even if providers exist.

2. **Network Errors**: If the provider fetch fails, the feature fails gracefully by showing an empty list.

3. **No Pre-selection**: The system does not automatically select a provider, even if there's only one available.

4. **Provider Data**: The provider list shows the cost/price specific to that refaccion-provider relationship.

## Success Criteria

The feature is working correctly if:
- ✅ Providers button appears only when refaccion has providers
- ✅ Clicking the button loads and displays providers
- ✅ Users can select a provider from the list
- ✅ Selected provider is visually indicated
- ✅ Cargo is created with correct provider ID when provider is selected
- ✅ Cargo is created with NULL provider ID when no provider is selected
- ✅ Changing refaccion clears the provider selection
- ✅ Error handling works gracefully (no crashes on API failures)

## Troubleshooting

### Providers button doesn't appear:
- Check if the refaccion actually has providers in the database
- Check the debug console for errors from `CheckProveedorExistsAsync`

### Empty provider list:
- Verify providers exist for that refaccion in the database
- Check the debug console for errors from `GetProveedoresByRefaccionAsync`
- Verify API endpoint is accessible

### Provider ID not saved:
- Verify you actually selected a provider from the list
- Check that the provider was still selected when clicking "Agregar"
- Verify the API accepts the `idProveedor` parameter
