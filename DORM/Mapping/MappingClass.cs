using DORM.Attribute;
using DORM.Mapping.Reflect;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace DORM.Mapping
{
    public class MappingClass
    {   
        public static List<TableField> MapClass<T>() where T : class
        {
            Type type = typeof(T);
            List<TableField> table = new List<TableField>();

            foreach (PropertyInfo info in type.GetProperties())
            {
                var pName = info.Name;
                var pType = info.PropertyType.Name;
                var pNullAllowed = CheckNullInProperty(info);
                TableField tField = new TableField(info.Name, pType, pNullAllowed);
                CheckAttributes(info, tField);
                table.Add(tField);
            }
            return table;
        }


        private static bool CheckNullInProperty(PropertyInfo info)
        {
            var context = new NullabilityInfoContext();
            var check = context.Create(info);
            if (check.WriteState == NullabilityState.Nullable)
            {
                return true;
            }
            return false;
        }


        private static void CheckAttributes(PropertyInfo info, TableField tField)
        {
            System.Attribute[] attributes = System.Attribute.GetCustomAttributes(info);
            foreach (var attr in attributes)
            {
                if (attr is IApplyAttribute applyAttribute)
                {
                    applyAttribute.Apply(tField, info);
                }
            }
        }

    }
}
