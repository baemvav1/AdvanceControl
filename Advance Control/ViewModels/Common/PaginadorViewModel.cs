using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Advance_Control.ViewModels.Common
{
    /// <summary>
    /// Contrato no genérico de <see cref="PaginadorViewModel{T}"/>, usado por
    /// <see cref="Advance_Control.Views.Controls.PaginacionFooterControl"/> para
    /// enlazar el mismo footer de paginación sin importar el tipo de catálogo.
    /// </summary>
    public interface IPaginador
    {
        int PageIndex { get; }
        int TotalPages { get; }
        string PaginaTexto { get; }
        string RangoTexto { get; }
        bool CanIrAnterior { get; }
        bool CanIrSiguiente { get; }
        void IrPrimera();
        void IrAnterior();
        void IrSiguiente();
        void IrUltima();
    }

    /// <summary>
    /// Paginación del lado del cliente para catálogos: recibe la lista completa
    /// ya filtrada (<see cref="EstablecerElementos"/>) y expone solo la página
    /// actual en <see cref="PageItems"/>, más el texto de rango para el footer
    /// ("1–15 de 89") y los controles de navegación entre páginas.
    /// </summary>
    public class PaginadorViewModel<T> : ViewModelBase, IPaginador
    {
        private List<T> _todos = new();

        public int PageSize { get; }

        public PaginadorViewModel(int pageSize = 15)
        {
            PageSize = pageSize;
        }

        public ObservableCollection<T> PageItems { get; } = new();

        private int _pageIndex = 1;
        public int PageIndex
        {
            get => _pageIndex;
            private set
            {
                if (SetProperty(ref _pageIndex, value))
                {
                    OnPropertyChanged(nameof(RangoTexto));
                    OnPropertyChanged(nameof(PaginaTexto));
                    OnPropertyChanged(nameof(CanIrAnterior));
                    OnPropertyChanged(nameof(CanIrSiguiente));
                }
            }
        }

        private int _totalItems;
        public int TotalItems
        {
            get => _totalItems;
            private set
            {
                if (SetProperty(ref _totalItems, value))
                {
                    OnPropertyChanged(nameof(TotalPages));
                    OnPropertyChanged(nameof(RangoTexto));
                    OnPropertyChanged(nameof(PaginaTexto));
                    OnPropertyChanged(nameof(CanIrAnterior));
                    OnPropertyChanged(nameof(CanIrSiguiente));
                }
            }
        }

        public int TotalPages => TotalItems == 0 ? 1 : (int)Math.Ceiling(TotalItems / (double)PageSize);

        public bool CanIrAnterior => PageIndex > 1;
        public bool CanIrSiguiente => PageIndex < TotalPages;

        public string PaginaTexto => $"Página {PageIndex} de {TotalPages}";

        /// <summary>
        /// Leyenda de rango mostrado/total, ej. "1–15 de 89". "0 de 0" cuando no hay resultados.
        /// </summary>
        public string RangoTexto
        {
            get
            {
                if (TotalItems == 0)
                    return "0 de 0";

                var desde = (PageIndex - 1) * PageSize + 1;
                var hasta = Math.Min(PageIndex * PageSize, TotalItems);
                return $"{desde}–{hasta} de {TotalItems}";
            }
        }

        /// <summary>
        /// Reemplaza la lista completa (ya filtrada) y vuelve a la primera página.
        /// Llamar cada vez que cambie el resultado de la búsqueda/filtro.
        /// </summary>
        public void EstablecerElementos(IEnumerable<T> items)
        {
            _todos = items?.ToList() ?? new List<T>();
            TotalItems = _todos.Count;
            PageIndex = 1;
            Recalcular();
        }

        public void IrPrimera()
        {
            if (PageIndex == 1) return;
            PageIndex = 1;
            Recalcular();
        }

        public void IrAnterior()
        {
            if (!CanIrAnterior) return;
            PageIndex--;
            Recalcular();
        }

        public void IrSiguiente()
        {
            if (!CanIrSiguiente) return;
            PageIndex++;
            Recalcular();
        }

        public void IrUltima()
        {
            if (PageIndex == TotalPages) return;
            PageIndex = TotalPages;
            Recalcular();
        }

        private void Recalcular()
        {
            PageItems.Clear();
            foreach (var item in _todos.Skip((PageIndex - 1) * PageSize).Take(PageSize))
                PageItems.Add(item);
        }
    }
}
