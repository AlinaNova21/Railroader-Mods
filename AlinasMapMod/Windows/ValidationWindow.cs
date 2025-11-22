using System.Collections.Generic;
using System.Linq;
using AlinasMapMod.Validation;
using UI;
using UI.Builder;
using UI.Common;
using UnityEngine;

namespace AlinasMapMod.Windows;

internal class ValidationWindow : WindowBase
{
    private ValidationResult _validationResult;
    private bool _showErrors = true;
    private bool _showWarnings = true;
    private string _filterText = "";

    public override string WindowIdentifier => "ValidationWindow";
    public override string Title => "Validation Results";
    public override Vector2Int DefaultSize => new Vector2Int(600, 400);
    public override Window.Position DefaultPosition => Window.Position.CenterRight;
    public override Window.Sizing Sizing => Window.Sizing.Resizable(new Vector2Int(400, 300), new Vector2Int(800, 600));

    public void SetValidationResult(ValidationResult result)
    {
        _validationResult = result;
        Rebuild();
    }

    public void Show()
    {
        Rebuild();
        Window.ShowWindow();
    }

    public void Close()
    {
        Window.CloseWindow();
    }

    public override void Populate(UIPanelBuilder builder)
    {
        if (_validationResult == null)
        {
            builder.AddLabel("No validation results to display.");
            return;
        }

        BuildHeader(builder);
        BuildControls(builder);
        BuildResultsList(builder);
    }

    private void BuildHeader(UIPanelBuilder builder)
    {
        var errorCount = _validationResult.Errors?.Count ?? 0;
        var warningCount = _validationResult.Warnings?.Count ?? 0;
        var isValid = _validationResult.IsValid;

        builder.HStack(headerBuilder =>
        {
            headerBuilder.AddLabel($"Status: {(isValid ? "Valid" : "Invalid")}");
            headerBuilder.Spacer();
            headerBuilder.AddLabel($"Errors: {errorCount}");
            headerBuilder.Spacer();
            headerBuilder.AddLabel($"Warnings: {warningCount}");
            headerBuilder.Spacer();
            headerBuilder.AddButtonCompact("Clear", () => {
                _validationResult = null;
                Rebuild();
            });
        });

        builder.Spacer(10);
    }

    private void BuildControls(UIPanelBuilder builder)
    {
        builder.AddSection("Filters", sectionBuilder =>
        {
            sectionBuilder.HStack(controlsBuilder =>
            {
                controlsBuilder.AddField("Show Errors", controlsBuilder.AddToggle(() => _showErrors, value => {
                    _showErrors = value;
                    Rebuild();
                }));
                
                controlsBuilder.Spacer();
                
                controlsBuilder.AddField("Show Warnings", controlsBuilder.AddToggle(() => _showWarnings, value => {
                    _showWarnings = value;
                    Rebuild();
                }));
            });

            // Simple approach for now - just display current filter text
            sectionBuilder.AddLabel($"Filter: {_filterText}");
        });

        builder.Spacer(10);
    }

    private void BuildResultsList(UIPanelBuilder builder)
    {
        var filteredItems = GetFilteredItems();

        if (!filteredItems.Any())
        {
            builder.AddLabel("No items match the current filters.");
            return;
        }

        builder.VScrollView(scrollBuilder =>
        {
            foreach (var item in filteredItems)
            {
                BuildResultItem(scrollBuilder, item);
            }
        });
    }

    private void BuildResultItem(UIPanelBuilder builder, ValidationItem item)
    {
        var prefix = item.IsError ? "❌" : "⚠️";
        var title = string.IsNullOrEmpty(item.Field) 
            ? $"{prefix} {item.Message}" 
            : $"{prefix} {item.Field}: {item.Message}";

        builder.AddSection(title, sectionBuilder =>
        {
            if (!string.IsNullOrEmpty(item.Field))
            {
                sectionBuilder.AddLabel($"Field: {item.Field}");
            }

            if (!string.IsNullOrEmpty(item.Code))
            {
                sectionBuilder.AddLabel($"Code: {item.Code}");
            }

            if (item.Value != null)
            {
                sectionBuilder.AddLabel($"Value: {item.Value}");
            }

            sectionBuilder.AddLabel($"Message: {item.Message}");
        });

        builder.Spacer(5);
    }

    private List<ValidationItem> GetFilteredItems()
    {
        var items = new List<ValidationItem>();

        if (_showErrors && _validationResult.Errors != null)
        {
            items.AddRange(_validationResult.Errors.Select(e => new ValidationItem
            {
                IsError = true,
                Field = e.Field,
                Message = e.Message,
                Code = e.Code,
                Value = e.Value
            }));
        }

        if (_showWarnings && _validationResult.Warnings != null)
        {
            items.AddRange(_validationResult.Warnings.Select(w => new ValidationItem
            {
                IsError = false,
                Field = w.Field,
                Message = w.Message,
                Code = null,
                Value = w.Value
            }));
        }

        if (!string.IsNullOrEmpty(_filterText))
        {
            var filter = _filterText.ToLowerInvariant();
            items = items.Where(item =>
                (item.Field?.ToLowerInvariant().Contains(filter) ?? false) ||
                (item.Message?.ToLowerInvariant().Contains(filter) ?? false) ||
                (item.Code?.ToLowerInvariant().Contains(filter) ?? false)
            ).ToList();
        }

        return items;
    }

    private class ValidationItem
    {
        public bool IsError { get; set; }
        public string Field { get; set; }
        public string Message { get; set; }
        public string Code { get; set; }
        public object Value { get; set; }
    }
}