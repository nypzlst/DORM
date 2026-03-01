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
            string NameTablle = AttributeNameTable.Name ?? type.Name;


            foreach(PropertyInfo info in type.GetProperties())
            {
                var pType = info.PropertyType.Name;

                var pNullAllowed = CheckNullInProperty(info);
                
                var attrs = (NameAttribute)System.Attribute.GetCustomAttribute(info, typeof(NameAttribute));
                var pName = attrs.Name ?? info.Name;



                TableField tField = new TableField(pName,pType,pNullAllowed);
                table.Add(tField);

            }
        }

        public bool CheckNullInProperty(PropertyInfo info)
        {
            var context = new NullabilityInfoContext();
            var check = context.Create(info);
            if (check.WriteState == NullabilityState.Nullable)
            {
                return true;
            }
            return false;
        }


        public void CheckAttributes(PropertyInfo info, TableField tField)
        {
            System.Attribute[] attributes = System.Attribute.GetCustomAttributes(info);
            

            foreach(System.Attribute attr in attributes)
            {
                //проверку сделать или ифом или свичем
                switch (attr)
                {
                    case NameAttribute name:
                        tField.FieldName = name.Name ?? info.Name;
                        break;
                    case UniqueAttribute unique:
                        tField.isUnique = true;
                        break;
                    case PKAttribute primaryKey:
                        tField.isPrimaryKey = true;
                        break;
                    case DefaultAttribute da:

                }
            }

        }
    }



}
