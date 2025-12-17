# Login Dialog UI Changes - Visual Documentation

## Overview
The login dialog now has two distinct states based on the user's authentication status.

## State 1: Not Authenticated (Login Form)

When the user is **not authenticated**, the dialog shows the standard login form:

```
┌────────────────────────────────────────┐
│         Iniciar Sesión                 │
├────────────────────────────────────────┤
│                                        │
│  Usuario:                              │
│  ┌──────────────────────────────────┐ │
│  │ Ingrese su nombre de usuario     │ │
│  └──────────────────────────────────┘ │
│                                        │
│  Contraseña:                           │
│  ┌──────────────────────────────────┐ │
│  │ Ingrese su contraseña            │ │
│  └──────────────────────────────────┘ │
│                                        │
│  ┌────────────────────────────────┐   │
│  │ ⓘ [Error message if present]  │   │
│  └────────────────────────────────┘   │
│                                        │
│              [Iniciar Sesión] [Cancelar]│
└────────────────────────────────────────┘
```

### UI Elements:
- **Title**: "Iniciar Sesión"
- **Username Field**: TextBox with placeholder "Ingrese su nombre de usuario"
- **Password Field**: PasswordBox with placeholder "Ingrese su contraseña"
- **Error Display**: InfoBar (Severity: Error) - shown only when there's an error
- **Primary Button**: "Iniciar Sesión" (Accent style, enabled when both fields have valid input)
- **Secondary Button**: "Cancelar" (Default style)

### Behavior:
- Username and password fields use two-way binding with property change updates
- "Iniciar Sesión" button is disabled if:
  - Username is empty or has less than 3 characters
  - Password is empty or has less than 4 characters
  - IsLoading is true (operation in progress)
- On successful login:
  - Dialog closes automatically
  - User is notified via notification service
  - MainViewModel updates authentication state
- On failed login:
  - Error message displays in InfoBar
  - Fields remain populated for retry
- On cancel:
  - Form is cleared
  - Dialog closes

## State 2: Already Authenticated (Logout Panel)

When the user is **already authenticated**, the dialog shows the logout panel:

```
┌────────────────────────────────────────┐
│         Iniciar Sesión                 │
├────────────────────────────────────────┤
│                                        │
│   Ya tiene una sesión activa           │
│                                        │
│  ┌────────────────────────────────┐   │
│  │ ℹ️ Ya se encuentra autenticado.│   │
│  │   Puede cerrar esta ventana o  │   │
│  │   cerrar sesión.               │   │
│  └────────────────────────────────┘   │
│                                        │
│  ┌────────────────────────────────┐   │
│  │ ⓘ [Error message if present]  │   │
│  └────────────────────────────────┘   │
│                                        │
│              [Cerrar Sesión] [Cerrar]  │
└────────────────────────────────────────┘
```

### UI Elements:
- **Title**: "Iniciar Sesión" (dialog title remains the same)
- **Status Text**: "Ya tiene una sesión activa" (centered, semibold, size 16)
- **Info Display**: InfoBar (Severity: Informational, always visible)
  - Message: "Ya se encuentra autenticado. Puede cerrar esta ventana o cerrar sesión."
- **Error Display**: InfoBar (Severity: Error) - shown only when there's a logout error
- **Primary Button**: "Cerrar Sesión" (Accent style, enabled when not loading)
- **Secondary Button**: "Cerrar" (Default style)

### Behavior:
- Dialog automatically detects authentication state on open via `RefreshAuthenticationState()`
- "Cerrar Sesión" button is disabled if:
  - IsLoading is true (operation in progress)
  - User is not authenticated (shouldn't happen but handled)
- On successful logout:
  - Local tokens are cleared
  - Server-side refresh token is revoked
  - Dialog closes automatically
  - MainViewModel updates authentication state
  - User info (initials, type) is cleared from main UI
  - User is notified via notification service
- On failed logout:
  - Error message displays in InfoBar
  - Local tokens are still cleared (idempotent behavior)
  - User can retry or close
- On close:
  - Dialog closes without any changes
  - User remains authenticated

## Technical Implementation

### Conditional Visibility
The two panels use conditional visibility binding:

**Login Form Panel:**
```xaml
<Grid Visibility="{x:Bind ViewModel.IsNotAuthenticated, Mode=OneWay, 
                   Converter={StaticResource BoolToVisibilityConverter}}">
```

**Logout Panel:**
```xaml
<Grid Visibility="{x:Bind ViewModel.IsAuthenticated, Mode=OneWay, 
                   Converter={StaticResource BoolToVisibilityConverter}}">
```

### Properties Used
- `ViewModel.IsAuthenticated`: Boolean indicating current auth state
- `ViewModel.IsNotAuthenticated`: Computed property (!IsAuthenticated) for cleaner binding
- `ViewModel.IsLoading`: Boolean indicating operation in progress
- `ViewModel.HasError`: Boolean indicating if error message exists
- `ViewModel.ErrorMessage`: String with error details

### Commands
- `LoginCommand`: RelayCommand for login operation (async void pattern)
- `LogoutCommand`: AsyncRelayCommand for logout operation (async Task pattern)

## State Transitions

### User Flow 1: Login from Unauthenticated State
```
[Not Authenticated] 
    → User opens dialog 
    → Sees Login Form 
    → Enters credentials 
    → Clicks "Iniciar Sesión" 
    → [Loading state with disabled button]
    → [Authenticated] 
    → Dialog closes automatically
    → Main UI shows user info
```

### User Flow 2: Logout from Authenticated State
```
[Authenticated] 
    → User opens dialog 
    → Sees Logout Panel 
    → Clicks "Cerrar Sesión" 
    → [Loading state with disabled button]
    → Server revokes refresh token
    → Local tokens cleared
    → [Not Authenticated] 
    → Dialog closes automatically
    → Main UI clears user info
```

### User Flow 3: Open Dialog While Authenticated
```
[Authenticated] 
    → User opens dialog 
    → Dialog calls RefreshAuthenticationState()
    → Sees Logout Panel with info message
    → User can choose:
        - Click "Cerrar Sesión" → Logout flow
        - Click "Cerrar" → Dialog closes, remains authenticated
```

## Error Handling

### Login Errors
- Invalid credentials → "Usuario o contraseña incorrectos."
- Validation errors → Specific message (e.g., "El nombre de usuario debe tener al menos 3 caracteres.")
- Network timeout (30s) → "Autenticación excedió el tiempo de espera (30 segundos)"
- Connection error → "Error de conexión al autenticar usuario"
- Generic error → "Error al iniciar sesión: [exception message]"

### Logout Errors
- Server error → "Error al cerrar sesión. Por favor, intente nuevamente."
- Network timeout (15s) → Logged but local cleanup succeeds
- Connection error → Logged but local cleanup succeeds
- Generic error → "Error al cerrar sesión: [exception message]"

**Note**: Logout always succeeds locally (idempotent behavior) even if server communication fails.

## Performance Characteristics

### Timeout Values
- **Login**: 30 seconds maximum wait time
- **Logout**: 15 seconds maximum wait time
- **Token Refresh**: 15 seconds maximum wait time
- **Token Validation**: 10 seconds maximum wait time

### Responsiveness
- UI remains responsive during operations (async/await pattern)
- Buttons disabled during operations to prevent double-submission
- Operations can be cancelled via CancellationToken
- Development mode can disable timeouts for debugging

## Accessibility

### Keyboard Navigation
- Tab order: Username → Password → Login Button → Cancel Button
- Enter key in password field triggers login
- Escape key closes dialog (standard dialog behavior)

### Screen Reader Support
- All labels properly associated with inputs
- InfoBar messages announced to screen readers
- Button states (enabled/disabled) properly communicated

## Browser/Platform Support
- Windows 10 version 1809 (10.0.17763.0) or higher
- WinUI 3 / Windows App SDK
- .NET 8.0

## Future Enhancements (Not Included)
Potential improvements that could be added in the future:
- Remember username checkbox
- Password visibility toggle
- "Forgot password" link
- Multiple account switching
- Session timeout countdown display
- Biometric authentication option
