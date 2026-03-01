using DORM.Attribute;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace DORM.Mapping
{

    /// <summary>
    /// Клас котрий відповідає за рефлексію пов'язаною з таблицями бази даних
    /// </summary>

    public class TableReflect
    {
        /// <summary>
        /// Метод для створення таблиці, котрий отримує клас користувача
        /// </summary>
        /// <param name="obj">Клас котрий містить в собі параметри</param>
        public void CreateTable(Object obj)
        {
            Type type = obj.GetType();
            List<TableField> table = new List<TableField>();

            var AttributeNameTable = (NameAttribute)System.Attribute.GetCustomAttribute(type, typeof(NameAttribute));
            string NameTable = AttributeNameTable?.Name ?? type.Name;


            foreach(PropertyInfo info in type.GetProperties())
            {
                var pName = info.Name;
                var pType = info.PropertyType.Name;
                var pNullAllowed = CheckNullInProperty(info);              
                TableField tField = new TableField(info.Name,pType,pNullAllowed);

                CheckAttributes(info, tField);
                
                table.Add(tField);
            }
        }

        private bool CheckNullInProperty(PropertyInfo info)
        {
            var context = new NullabilityInfoContext();
            var check = context.Create(info);
            if (check.WriteState == NullabilityState.Nullable)
            {
                return true;
            }
            return false;
        }


        private void CheckAttributes(PropertyInfo info, TableField tField)
        {
            System.Attribute[] attributes = System.Attribute.GetCustomAttributes(info);
            foreach (var attr in attributes)
            {
                if(attr is IApplyAttribute applyAttribute)
                {
                    applyAttribute.Apply(tField, info);
                }
            }
        }
    }



}
