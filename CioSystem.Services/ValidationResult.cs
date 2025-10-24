using System.Collections.Generic;

namespace CioSystem.Services
{
    /// <summary>
    /// 驗證結果
    /// </summary>
    public class ValidationResult
    {
        /// <summary>
        /// 是否有效
        /// </summary>
        public bool IsValid { get; set; } = true;

        /// <summary>
        /// 錯誤訊息列表
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();

        /// <summary>
        /// 警告訊息列表
        /// </summary>
        public List<string> Warnings { get; set; } = new List<string>();

        /// <summary>
        /// 添加錯誤訊息
        /// </summary>
        /// <param name="error">錯誤訊息</param>
        public void AddError(string error)
        {
            Errors.Add(error);
            IsValid = false;
        }

        /// <summary>
        /// 添加警告訊息
        /// </summary>
        /// <param name="warning">警告訊息</param>
        public void AddWarning(string warning)
        {
            Warnings.Add(warning);
        }

        /// <summary>
        /// 添加多個錯誤訊息
        /// </summary>
        /// <param name="errors">錯誤訊息列表</param>
        public void AddErrors(IEnumerable<string> errors)
        {
            foreach (var error in errors)
            {
                AddError(error);
            }
        }

        /// <summary>
        /// 添加多個警告訊息
        /// </summary>
        /// <param name="warnings">警告訊息列表</param>
        public void AddWarnings(IEnumerable<string> warnings)
        {
            foreach (var warning in warnings)
            {
                AddWarning(warning);
            }
        }
    }
}