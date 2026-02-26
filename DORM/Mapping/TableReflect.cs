using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace DORM.Mapping
{
    public class TableReflect
    {
        public void CreateTable(Object obj)
        {
            Type type = obj.GetType();

            List<TableField> table = new List<TableField>();
            
            foreach(PropertyInfo info in  type.GetProperties())
            {
                var pName = info.Name;
                var pType = info.PropertyType.Name;

                var pNullAllowed = false;
                var context = new NullabilityInfoContext();
                var check = context.Create(info);
                if(check.ReadState == NullabilityState.Nullable)
                {
                    pNullAllowed = true;
                }

                TableField tField = new TableField(pName,pType,pNullAllowed);
                table.Add(tField);

            }
        }
    }
}
