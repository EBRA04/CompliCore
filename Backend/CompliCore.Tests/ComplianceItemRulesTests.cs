using CompliCore.Enums;
using CompliCore.Rules;
using Xunit;

namespace CompliCore.Tests
{
    public class ComplianceItemRulesTests
    {
        [Fact]
        public void EveryTypeExceptOtherIsClassified()
        {
            foreach (var type in Enum.GetValues<ComplianceItemType>())
            {
                if (type == ComplianceItemType.Other) continue;

                var classified = ComplianceItemRules.EmployeeLevel.Contains(type)
                              || ComplianceItemRules.CompanyLevel.Contains(type);

                Assert.True(classified, $"{type} is not classified");
            }
        }

        [Fact]
        public void NoTypeIsBothEmployeeAndCompanyLevel()
        {
            foreach (var type in Enum.GetValues<ComplianceItemType>())
            {
                var inEmployee = ComplianceItemRules.EmployeeLevel.Contains(type);
                var inCompany = ComplianceItemRules.CompanyLevel.Contains(type);

                Assert.False(inEmployee && inCompany, $"{type} is in both sets");
            }
        }
    }
}