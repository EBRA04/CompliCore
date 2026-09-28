using CompliCore.Enums;
using System.Text.RegularExpressions;
using static CompliCore.Enums.ComplianceItemType;
namespace CompliCore.Rules
{
   
    public static class ComplianceItemRules
    {
        public static string DisplayName(ComplianceItemType type) =>
        Regex.Replace(type.ToString(), "(?<=[a-z])(?=[A-Z])", " ");
        public static readonly HashSet<ComplianceItemType> EmployeeLevel = new() {Iqama,Passport,WorkPermit,HealthInsurance,EmploymentContract};
        public static readonly HashSet<ComplianceItemType> CompanyLevel = new() { CommercialRegistration,MunicipalLicense,CivilDefenseCertificate};
    }
}
