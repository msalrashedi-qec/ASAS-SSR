using Microsoft.JSInterop;
using Radzen.Blazor;

namespace Web.Components.Partial.Business;

public class SearchFocusedDropDown<TValue> : RadzenDropDown<TValue>
{
    private bool focusSearchAfterRender;

    protected override async Task OpenPopup(string key = "ArrowDown", bool isFilter = false, bool isFromClick = false)
    {
        await base.OpenPopup(key, isFilter, isFromClick);

        // The filter must be rendered before focus is applied, including on first open.
        focusSearchAfterRender = !Disabled && AllowFiltering && isPopupOpen && !isFilter;
        if (focusSearchAfterRender)
            StateHasChanged();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (focusSearchAfterRender)
        {
            focusSearchAfterRender = false;
            if (isPopupOpen && !Disabled)
                await JSRuntime.InvokeVoidAsync("focusDropdownSearch", PopupID, SearchID);
        }
    }
}
