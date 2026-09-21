using CompliCore.Enums;
using static CompliCore.Enums.ComplianceItemType;
namespace CompliCore.Rules
{
    public static class ComplianceItemRules
    {
        public static readonly HashSet<ComplianceItemType> EmployeeLevel = new() {Iqama,Passport,WorkPermit,HealthInsurance,EmploymentContract};
        public static readonly HashSet<ComplianceItemType> CompanyLevel = new() { CommercialRegistration,MunicipalLicense,CivilDefenseCertificate};
    }
}
