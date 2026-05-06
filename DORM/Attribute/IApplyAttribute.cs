using DORM.Mapping.Reflect;
using System.Reflection;


namespace DORM.Attribute
{
    public interface IApplyAttribute 
    {
        void Apply(TableField tField, PropertyInfo info);
    }
}
