namespace Advance_Control.Services.Dialog
{
    /// <summary>
    /// Interface for UserControls that can provide a result when used in a dialog
    /// </summary>
    /// <typeparam name="T">The type of result the dialog content provides</typeparam>
    public interface IDialogContent<T>
    {
        /// <summary>
        /// Gets the result value from the dialog content
        /// </summary>
        T GetResult();
    }
}
