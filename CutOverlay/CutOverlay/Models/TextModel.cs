using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CutOverlay.Models
{
    public class TextModel : PageModel
    {
        public string? Text { get; set; }
        public string? StyleId { get; set; }
        public string StyleIdDiv => StyleId + "Div";
        public string StyleIdText => StyleId;
        public string StyleIdShadow => StyleId + "Shadow"; 
    }
}
