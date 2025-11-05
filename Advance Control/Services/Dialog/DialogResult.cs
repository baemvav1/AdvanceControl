namespace Advance_Control.Services.Dialog
{
    /// <summary>
    /// Represents the result of a dialog operation with a typed value
    /// </summary>
    /// <typeparam name="T">The type of the result value</typeparam>
    public class DialogResult<T>
    {
        /// <summary>
        /// Gets whether the dialog was confirmed (primary button clicked)
        /// </summary>
        public bool IsConfirmed { get; set; }

        /// <summary>
        /// Gets the result value from the dialog
        /// </summary>
        public T? Result { get; set; }

        public DialogResult(bool isConfirmed, T? result = default)
        {
            IsConfirmed = isConfirmed;
            Result = result;
        }
    }
}
