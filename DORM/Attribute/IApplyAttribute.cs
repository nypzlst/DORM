using DORM.Mapping;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace DORM.Attribute
{
    public interface IApplyAttribute 
    {
        void Apply(TableField tField, PropertyInfo info);
    }
}
