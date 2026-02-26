using DORM.Attribute;
using System;
using System.Collections.Generic;
using System.Text;

namespace DORM.Example
{
    [Name("TUser")]
    public class User 
    {
        [PK]
        public int Id { get; set; }

        [Unique]
        public string? Email { get; set; }


    }
}
