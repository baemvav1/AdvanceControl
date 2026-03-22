using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Advance_Control.Models;
using Advance_Control.Services.Logging;

namespace Advance_Control.Services.PermisosUi
{
    public class PermisoUiScanner : IPermisoUiScanner
    {
        private static readonly XNamespace XamlNamespace = "http://schemas.microsoft.com/winfx/2006/xaml";
        private readonly ILoggingService _logger;

        public PermisoUiScanner(ILoggingService logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<PermisoModuloSyncDto>> ScanAsync(CancellationToken cancellationToken = default)
        {
            var projectRoot = ResolveProjectRoot();
            if (projectRoot == null)
            {
                await _logger.LogWarningAsync("No fue posible localizar la raíz del proyecto para escanear permisos UI.", "PermisoUiScanner", "ScanAsync");
                return new List<PermisoModuloSyncDto>();
            }

            var modules = new List<PermisoModuloSyncDto>();
            var targets = new[]
            {
                (Folder: "Views\\Pages", Group: "Pages"),
                (Folder: "Views\\Windows", Group: "Windows"),
                (Folder: "Views\\Dialogs", Group: "Dialogs")
            };

            for (var groupIndex = 0; groupIndex < targets.Length; groupIndex++)
            {
                var target = targets[groupIndex];
                var folderPath = Path.Combine(projectRoot, target.Folder);
                if (!Directory.Exists(folderPath))
                    continue;

                var files = Directory.EnumerateFiles(folderPath, "*.xaml", SearchOption.AllDirectories)
                    .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                for (var moduleIndex = 0; moduleIndex < files.Count; moduleIndex++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var file = files[moduleIndex];
                    var module = BuildModule(projectRoot, file, target.Group, groupIndex + 1, moduleIndex + 1);
                    if (module != null)
                    {
                        modules.Add(module);
                    }
                }
            }

            return modules;
        }

        private PermisoModuloSyncDto? BuildModule(string projectRoot, string filePath, string groupName, int orderGroup, int orderModule)
        {
            try
            {
                var document = XDocument.Load(filePath, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
                var root = document.Root;
                if (root == null)
                    return null;

                var xamlClass = root.Attribute(XamlNamespace + "Class")?.Value;
                if (string.IsNullOrWhiteSpace(xamlClass))
                    return null;

                var moduleKey = PermisoUiKeyBuilder.BuildModuleKey(xamlClass);
                var viewName = Path.GetFileNameWithoutExtension(filePath);
                var relativePath = Path.GetRelativePath(projectRoot, filePath);

                var module = new PermisoModuloSyncDto
                {
                    ClaveModulo = moduleKey,
                    GrupoModulo = groupName,
                    TagNavegacion = viewName,
                    NombreModulo = HumanizeViewName(viewName),
                    NombreView = viewName,
                    RutaView = relativePath,
                    OrdenGrupo = orderGroup,
                    OrdenModulo = orderModule
                };

                var actions = new List<PermisoAccionModuloSyncDto>();
                var actionOrder = 1;

                foreach (var element in root.Descendants())
                {
                    var controlType = element.Name.LocalName;
                    if (!PermisoUiKeyBuilder.SupportedActionTypes.Contains(controlType))
                        continue;

                    var controlKey = ResolveControlKey(element);
                    if (string.IsNullOrWhiteSpace(controlKey))
                        continue;

                    actions.Add(new PermisoAccionModuloSyncDto
                    {
                        ClaveAccion = PermisoUiKeyBuilder.BuildActionKey(moduleKey, controlType, controlKey),
                        NombreAccion = HumanizeControlKey(controlKey),
                        TipoAccion = controlType,
                        ControlKey = controlKey,
                        Descripcion = $"{controlType} detectado en {viewName}",
                        OrdenAccion = actionOrder++
                    });
                }

                module.Acciones = actions;
                return module;
            }
            catch (Exception ex)
            {
                _ = _logger.LogErrorAsync($"Error al escanear permisos UI en {filePath}", ex, "PermisoUiScanner", "BuildModule");
                return null;
            }
        }

        private static string? ResolveProjectRoot()
        {
            var candidates = new[]
            {
                AppContext.BaseDirectory,
                Environment.CurrentDirectory
            }
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Distinct(StringComparer.OrdinalIgnoreCase);

            foreach (var candidate in candidates)
            {
                var current = new DirectoryInfo(candidate);
                while (current != null)
                {
                    var projectFile = Path.Combine(current.FullName, "Advance Control.csproj");
                    if (File.Exists(projectFile))
                        return current.FullName;

                    current = current.Parent;
                }
            }

            return null;
        }

        private static string? ResolveControlKey(XElement element)
        {
            return FirstNonEmpty(
                element.Attribute(XamlNamespace + "Name")?.Value,
                element.Attribute("Name")?.Value,
                element.Attribute("Tag")?.Value,
                element.Attribute("Content")?.Value,
                element.Attribute("Text")?.Value,
                element.Attribute("Header")?.Value);
        }

        private static string? FirstNonEmpty(params string?[] values)
        {
            return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim();
        }

        private static string HumanizeViewName(string viewName)
        {
            return HumanizeControlKey(viewName
                .Replace("Page", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace("Window", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace("Dialog", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace("UserControl", string.Empty, StringComparison.OrdinalIgnoreCase));
        }

        private static string HumanizeControlKey(string controlKey)
        {
            if (string.IsNullOrWhiteSpace(controlKey))
                return string.Empty;

            var sanitized = controlKey
                .Replace("Button", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace("ComboBox", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace("Toggle", " Toggle ", StringComparison.OrdinalIgnoreCase)
                .Replace("_", " ");

            var chars = new List<char>(sanitized.Length + 8);
            for (var i = 0; i < sanitized.Length; i++)
            {
                var current = sanitized[i];
                if (i > 0 && char.IsUpper(current) && !char.IsWhiteSpace(sanitized[i - 1]) && !char.IsUpper(sanitized[i - 1]))
                    chars.Add(' ');

                chars.Add(current);
            }

            return new string(chars.ToArray()).Trim();
        }
    }
}
