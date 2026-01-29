# Testing Guide: Provider Functionality for Cargos

## Prerequisites
- Windows environment with WinUI 3 support
- .NET 8.0 SDK installed
- Access to the AdvanceControl database with test data

## Test Scenarios

### Scenario 1: Refaccion Selection with Auto-fill Monto

**Steps:**
1. Navigate to OperacionesView
2. Expand an operation to see its cargos
3. Click "Agregar Cargo" button
4. In the dialog, select "Refacción" from the "Tipo de Cargo" dropdown
5. Search for a refaccion using the search fields (marca or serie)
6. Click on a refaccion in the list

**Expected Results:**
- The refaccion list and search fields should disappear
- A card should appear showing the selected refaccion details:
  - Marca
  - Serie
  - Costo
  - "Cambiar" button
- The "Monto" field should be automatically filled with the refaccion's cost
- If the refaccion has associated providers, a "Proveedores" button should appear

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
│ [Proveedores] (if providers exist)  │
│                                     │
│ Monto: [150.00]                     │
│                                     │
│ Nota (opcional): [              ]  │
├─────────────────────────────────────┤
│              [Cancelar]  [Agregar]  │
└─────────────────────────────────────┘
```

### Scenario 2: Changing Refaccion Selection

**Steps:**
1. Follow Scenario 1 to select a refaccion
2. Click the "Cambiar" button

**Expected Results:**
- The selected refaccion card should disappear
- The search fields should reappear
- The refaccion list should be visible again
- The "Monto" field should be cleared (set to 0)
- The "Proveedores" button should disappear

### Scenario 3: Refaccion with Providers

**Steps:**
1. Follow Scenario 1 to select a refaccion that has associated providers
2. Observe the "Proveedores" button appears
3. Click the "Proveedores" button

**Expected Results:**
- A grid/panel should appear below the button
- The grid should display the text "Lista de proveedores"
- Clicking the button again should hide the grid

**Visual Layout:**
```
┌─────────────────────────────────────┐
│ [Proveedores]                       │
│ ┌─────────────────────────────────┐ │
│ │                                 │ │
│ │   Lista de proveedores          │ │
│ │                                 │ │
│ └─────────────────────────────────┘ │
└─────────────────────────────────────┘
```

### Scenario 4: Refaccion without Providers

**Steps:**
1. Follow Scenario 1 to select a refaccion that has NO associated providers

**Expected Results:**
- The "Proveedores" button should NOT appear
- All other functionality should work as expected

### Scenario 5: Servicio Selection

**Steps:**
1. Navigate to OperacionesView
2. Expand an operation to see its cargos
3. Click "Agregar Cargo" button
4. In the dialog, select "Servicio" from the "Tipo de Cargo" dropdown
5. Search for and select a service

**Expected Results:**
- The service is selected normally (list remains visible)
- A "Proveedores" button should appear
- When the cargo is saved, the `idProveedor` should be set to the operation's `idAtiende`

**Visual Layout:**
```
┌─────────────────────────────────────┐
│ Agregar Cargo                       │
├─────────────────────────────────────┤
│ ID Operación: 123                   │
│                                     │
│ Tipo de Cargo: [Servicio ▼]        │
│                                     │
│ [Buscar por concepto...]            │
│ [Buscar por descripción...]         │
│                                     │
│ ┌─────────────────────────────────┐ │
│ │ [1] Reparación General          │ │
│ │     Servicio de mantenimiento   │ │
│ │     Costo: $200.00              │ │← Selected
│ ├─────────────────────────────────┤ │
│ │ [2] Diagnóstico                 │ │
│ │     Revisión completa           │ │
│ │     Costo: $50.00               │ │
│ └─────────────────────────────────┘ │
│                                     │
│ [Proveedores]                       │
│                                     │
│ Monto: [200.00]                     │
│                                     │
│ Nota (opcional): [              ]  │
├─────────────────────────────────────┤
│              [Cancelar]  [Agregar]  │
└─────────────────────────────────────┘
```

### Scenario 6: Creating Cargo with Servicio

**Steps:**
1. Follow Scenario 5 to select a service
2. Enter a monto value
3. Optionally enter a note
4. Click "Agregar" button

**Expected Results:**
- The cargo should be created successfully
- In the database, the cargo record should have:
  - `idTipoCargo` = 2 (Servicio)
  - `idRelacionCargo` = the service ID
  - `idProveedor` = the operation's `idAtiende` value
- A success notification should appear
- The cargos list should refresh showing the new cargo

### Scenario 7: Error Handling - Failed Provider Check

**Steps:**
1. Simulate a network error or API failure
2. Try to select a refaccion

**Expected Results:**
- The refaccion should still be selected and shown
- The "Proveedores" button should NOT appear (fails safely)
- An error message should be logged to debug console
- The user can still create the cargo normally

## Database Verification

After creating cargos, verify the following in the database:

### For Refaccion Cargos:
```sql
SELECT IdCargo, IdTipoCargo, IdRelacionCargo, Monto, IdProveedor
FROM Cargos
WHERE IdOperacion = [test_operation_id]
AND IdTipoCargo = 1;
```

**Expected:**
- `IdTipoCargo` should be 1
- `IdRelacionCargo` should match the refaccion ID
- `Monto` should match the refaccion's cost
- `IdProveedor` should be NULL (not set for refacciones)

### For Servicio Cargos:
```sql
SELECT c.IdCargo, c.IdTipoCargo, c.IdRelacionCargo, c.Monto, c.IdProveedor, o.IdAtiende
FROM Cargos c
INNER JOIN Operaciones o ON c.IdOperacion = o.IdOperacion
WHERE c.IdOperacion = [test_operation_id]
AND c.IdTipoCargo = 2;
```

**Expected:**
- `IdTipoCargo` should be 2
- `IdRelacionCargo` should match the service ID
- `IdProveedor` should equal `o.IdAtiende`

## Edge Cases to Test

1. **Refaccion with cost = 0:**
   - Select a refaccion with cost of 0
   - Verify monto is set to 0 (not NaN or empty)

2. **Refaccion with null cost:**
   - Select a refaccion where cost is NULL in database
   - Verify the UI handles this gracefully (shows $0 or appropriate default)

3. **Operation without idAtiende:**
   - Create a cargo for a servicio when the operation has NULL idAtiende
   - Verify the cargo is created with NULL idProveedor

4. **Network timeout on CheckProveedorExistsAsync:**
   - Simulate slow/timeout network
   - Verify the UI doesn't hang and fails gracefully

## Performance Testing

- Select multiple refacciones in sequence (change selections rapidly)
- Verify each selection correctly updates the UI
- Check that previous API calls are properly handled

## Accessibility Testing

- Test keyboard navigation through the dialog
- Verify Tab order is logical
- Test with screen reader to ensure all elements are announced properly
- Verify button labels are descriptive

## Localization Note

The current implementation uses Spanish text for the provider grid placeholder:
- "Lista de proveedores"

When implementing the full provider list functionality, ensure all text is properly localized.
