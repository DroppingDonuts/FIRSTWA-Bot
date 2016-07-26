using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using System.Globalization;
using System.ComponentModel;

namespace VolunteerDataWebApi.Models
{
    public class Volunteer
    { 
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Volunteer()
        {
            this.VolunteerActivities = new HashSet<VolunteerActivity>();
            this.VolunteerIntents = new HashSet<VolunteerIntent>();
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string PostalCode { get; set; }
        public bool CanMessage { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<VolunteerActivity> VolunteerActivities { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<VolunteerIntent> VolunteerIntents { get; set; }
    }
}