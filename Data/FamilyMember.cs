using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace PolarExpress3.Data
{
    public class Family
    {
        public Family()
        {
            Members = new List<FamilyMember>();
        }
            
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [Required]
        [StringLength(20, ErrorMessage = "Name is too long.")]
        public string FamilyID { get; set; }
        public List<FamilyMember> Members { get; set; }
    }

    public class FamilyMember
    {
        public FamilyMember()
        {            
        }

        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
        public string FamilyID { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }        
    }

    public class GiftRequest
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
        public string MemberID { get; set; }

        [Display(Name ="Name")]
        public string ShortName { get; set; }

        [Display(Name = "Description")]
        public string Description { get; set; }

        [Display(Name = "Web Link to Product")]
        public string ProductURL { get; set; }

        [Display(Name = "Estimated cost")]
        public double EstCost { get; set; }
        public string Base64Thumbnail { get; set; }
        public string GiverId { get; set; }

        private string _mimeType;
        public string ThumbnailMimeType
        {
            get
            {
                return (String.IsNullOrWhiteSpace(_mimeType) ? "image/jpeg" : _mimeType);
            }
            set
            {
                _mimeType = value;
            }
        }

    }

    public class UnreserveEvent
    {
        public string RequestID { get; set; }
        public string Email { get; set; }
    }
}
