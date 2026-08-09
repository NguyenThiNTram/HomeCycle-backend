using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Domain.Enums
{
    public enum BusinessOnboardingActionRoute
    {
        // Trang đăng ký hồ sơ - dùng CHUNG cho cả MissingProfile (tạo mới) và Rejected (nộp lại).
        OnboardingForm = 0,

        // Trang khảo sát nhu cầu thu mua - dùng cho SurveyPending.
        SurveyForm = 1
    }
}
