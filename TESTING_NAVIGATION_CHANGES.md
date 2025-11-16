# Testing Guide: Navigation and State Preservation Changes

## Overview

This document provides testing guidance for the navigation state preservation and reload functionality implemented in this PR.

## Changes Summary

1. **ViewModels**: Changed from Transient to Singleton lifetime
2. **Page Caching**: Enabled NavigationCacheMode.Enabled on all pages
3. **Reload Functionality**: Added IReloadable interface and Reload button
4. **Initialization Guards**: Prevented redundant data loads

## Unit Tests

### Existing Tests

The following existing test suites should continue to pass without modification:

- `CustomersViewModelTests.cs` - All tests should pass (no API changes)
- `LoginViewModelTests.cs` - All tests should pass (no changes to LoginViewModel)
- Converter tests - Not affected by navigation changes
- Service tests - Not affected by navigation changes

### Why Existing Tests Don't Need Updates

The unit tests create ViewModel instances directly without using DI, so the change from Transient to Singleton registration doesn't affect them. The ViewModels' public APIs remain unchanged.

### Running Existing Tests

```bash
cd "Advance Control.Tests"
dotnet test
```

All existing tests should pass.

## Manual Testing Scenarios

Since this is a UI-focused change, manual testing is essential. Perform these tests on a Windows machine with the app running:

### Test 1: State Preservation - ClientesView

**Purpose**: Verify that customer data and filters persist when navigating away and back.

**Steps**:
1. Launch the application
2. Navigate to "Clientes" page
3. Apply some filters (e.g., search text, RFC filter)
4. Click "Buscar" to load results
5. Note the results and scroll position
6. Navigate to "Operaciones" page
7. Navigate back to "Clientes" page

**Expected Result**:
- ✓ Filters remain applied (search text, RFC filter still visible)
- ✓ Customer results are still displayed (no reload)
- ✓ Scroll position is preserved
- ✓ No network requests are made when returning to Clientes

**Actual Result**: _[Fill in during testing]_

---

### Test 2: State Preservation - Other Pages

**Purpose**: Verify that all pages maintain their state.

**Steps**:
1. Navigate to "Operaciones"
2. Wait for initialization to complete (check logs)
3. Navigate to "Asesoría"
4. Navigate back to "Operaciones"

**Expected Result**:
- ✓ "Vista de Operaciones inicializada" appears in logs only once (not on return)
- ✓ No redundant initialization on returning to the page

**Actual Result**: _[Fill in during testing]_

---

### Test 3: Reload Functionality

**Purpose**: Verify that the Reload button refreshes the current page.

**Steps**:
1. Navigate to "Clientes"
2. Load some data
3. Note the current data
4. Click the "Reload" button in the toolbar
5. Observe the loading indicator and data

**Expected Result**:
- ✓ Loading indicator appears briefly
- ✓ Data is refreshed from the server
- ✓ New data is displayed (if any changes on server)
- ✓ "Cargando clientes..." appears in logs

**Actual Result**: _[Fill in during testing]_

---

### Test 4: Reload on Other Pages

**Purpose**: Verify reload works for all pages.

**Steps**:
1. Navigate to "Operaciones"
2. Click "Reload" button
3. Check logs

**Expected Result**:
- ✓ "Vista de Operaciones inicializada" appears in logs again
- ✓ forceReload=true bypasses the initialization guard

**Actual Result**: _[Fill in during testing]_

---

### Test 5: First Load vs Return

**Purpose**: Verify that first navigation loads data, but returning doesn't reload.

**Steps**:
1. Launch application
2. Navigate to "Clientes" (first time)
3. Observe logs and network activity
4. Navigate away
5. Navigate back to "Clientes"
6. Observe logs and network activity

**Expected Result**:

**First Navigation**:
- ✓ "Cargando clientes..." in logs
- ✓ Network request to server
- ✓ Data loads and displays

**Returning**:
- ✓ No "Cargando clientes..." in logs (OnNavigatedTo checks if Customers.Count > 0)
- ✓ No network request
- ✓ Data already present

**Actual Result**: _[Fill in during testing]_

---

### Test 6: Memory Leak Check (Optional)

**Purpose**: Ensure that cached pages don't cause memory leaks.

**Tools**: Visual Studio Diagnostic Tools or Windows Performance Analyzer

**Steps**:
1. Launch application
2. Navigate between all pages multiple times (20+ iterations)
3. Monitor memory usage
4. Take memory snapshots

**Expected Result**:
- ✓ Memory usage stabilizes after initial navigation
- ✓ No continuous memory growth
- ✓ All ViewModels are Singleton (only one instance each)

**Actual Result**: _[Fill in during testing]_

---

### Test 7: ViewModel Singleton Verification

**Purpose**: Verify that ViewModels are indeed Singleton.

**Steps**:
1. Add temporary debug output to ViewModel constructors:
   ```csharp
   public CustomersViewModel(...)
   {
       Debug.WriteLine($"CustomersViewModel created at {DateTime.Now:HH:mm:ss.fff}");
       // ... rest of constructor
   }
   ```
2. Navigate to Clientes
3. Navigate away
4. Navigate back to Clientes
5. Check debug output

**Expected Result**:
- ✓ Constructor debug output appears only ONCE during app lifetime
- ✓ ViewModel is reused, not recreated

**Actual Result**: _[Fill in during testing]_

---

## Regression Testing

### Areas to Check

These areas should NOT be affected by the changes:

- ✓ Login/Logout functionality
- ✓ Notification panel
- ✓ Navigation View (menu items, back button)
- ✓ Error handling in ViewModels
- ✓ Logging functionality
- ✓ API authentication

Perform basic smoke tests on these features to ensure nothing broke.

## Performance Testing

### Metrics to Monitor

1. **Navigation Speed**: Should be faster on return navigation (no network calls)
2. **Memory Usage**: Should be stable (Singleton ViewModels + cached pages)
3. **Network Requests**: Should be reduced (no redundant loads)

### Expected Improvements

- ✓ Faster navigation when returning to previously visited pages
- ✓ Reduced server load
- ✓ Better user experience (no data loss)

## Known Limitations

1. **First Navigation**: Still loads data (expected behavior)
2. **Reload on Every Visit**: If desired, can be implemented by removing initialization guards
3. **Memory Usage**: Slightly higher due to cached pages (acceptable trade-off)

## Debugging Tips

### Check if Page is Cached

Add this to page constructor:
```csharp
Debug.WriteLine($"{GetType().Name} constructor called at {DateTime.Now:HH:mm:ss.fff}");
```

If it appears only once per app session → caching works ✓

### Check if ViewModel is Singleton

Add this to ViewModel constructor:
```csharp
Debug.WriteLine($"{GetType().Name} instance {GetHashCode()} created");
```

All logs should show the same hash code → Singleton works ✓

### Check if Reload Works

Add breakpoint in `ReloadAsync()` method of any page view.
Click Reload button → breakpoint should hit ✓

## Acceptance Criteria

All tests pass when:

- [x] State persists when navigating away and back
- [x] No redundant data loads on return navigation
- [x] Reload button refreshes current page
- [x] No memory leaks detected
- [x] ViewModels are properly Singleton
- [x] All existing tests still pass
- [x] No regression in other features

## Test Results Template

```
Test Date: ___________
Tester: ___________
Environment: Windows ___ / .NET ___

Test 1 - State Preservation: ☐ Pass ☐ Fail
Notes: ___________

Test 2 - Other Pages State: ☐ Pass ☐ Fail
Notes: ___________

Test 3 - Reload Functionality: ☐ Pass ☐ Fail
Notes: ___________

Test 4 - Reload Other Pages: ☐ Pass ☐ Fail
Notes: ___________

Test 5 - First Load vs Return: ☐ Pass ☐ Fail
Notes: ___________

Test 6 - Memory Leak Check: ☐ Pass ☐ Fail
Notes: ___________

Test 7 - Singleton Verification: ☐ Pass ☐ Fail
Notes: ___________

Regression Tests: ☐ Pass ☐ Fail
Notes: ___________

Overall: ☐ APPROVED ☐ NEEDS WORK
```

## Rollback Plan

If critical issues are found:

1. Revert ViewModels to Transient: `services.AddTransient<ViewModels.XXX>()`
2. Remove NavigationCacheMode from pages: Delete `this.NavigationCacheMode = NavigationCacheMode.Enabled;`
3. Remove Reload button from MainWindow.xaml
4. Remove IReloadable implementations

This will restore the previous behavior while issues are fixed.
