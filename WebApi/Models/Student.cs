//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace WebApi.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class Student
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Student()
        {
            this.FavouriteStops = new HashSet<FavouriteStop>();
            this.StudentHistories = new HashSet<StudentHistory>();
        }
    
        public int passid { get; set; }
        public string name { get; set; }
        public string gender { get; set; }
        public string regno { get; set; }
        public string contact { get; set; }
        public string qrcode { get; set; }
        public Nullable<int> totalJourney { get; set; }
        public Nullable<int> remainingJourney { get; set; }
        public Nullable<System.DateTime> passExpiray { get; set; }
        public Nullable<int> parent_id { get; set; }
        public Nullable<int> user_id { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<FavouriteStop> FavouriteStops { get; set; }
        public virtual Parent Parent { get; set; }
        public virtual User User { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<StudentHistory> StudentHistories { get; set; }
    }
}