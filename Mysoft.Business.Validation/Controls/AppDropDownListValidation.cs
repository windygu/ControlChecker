﻿using Mysoft.Business.Controls;
using Mysoft.Business.Validation.Entity;
using Mysoft.Common.Extensions;

namespace Mysoft.Business.Validation.Controls
{
    public class AppDropDownListValidation : AppValidationBase
    {
        public override void Validate(AppControl control)
        {
            var ddl = control.Control as AppDropDownList;
            if(ddl == null) return;

            if (control.DataSource != null && control.DataSource.Sql.IsNotNull())
            {
                //SQL验证都没通过就不用往下了
                if (!control.State.IsSqlPassed) return;

                var fields = GetFields(control.DataSource.Sql);
                bool textfound = false;
                bool valuefound = false;

                foreach (var field in fields)
                {
                    if(!textfound)
                    {
                        textfound = ddl.TextField.Text.EqualIgnoreCase(field);
                    }

                    if (!valuefound)
                    {
                        valuefound = ddl.ValueField.Text.EqualIgnoreCase(field);
                    }
                }

                if(!textfound)
                {
                    Results.Add(new Result("AppDropDownList", "SQL中未包含显示文本字段" + ddl.TextField.Text, Level.Error, typeof(AppDropDownListValidation)));
                }

                if (!valuefound)
                {
                    Results.Add(new Result("AppDropDownList", "SQL中未包含值字段" + ddl.ValueField.Text, Level.Error, typeof(AppDropDownListValidation)));
                }
            }
            else
            {
                Results.Add(new Result("AppDropDownList", "未配置数据源", Level.Warn, typeof(AppDropDownListValidation)));
            }
        }
    }
}