namespace CompliCore.Enums;

public enum ComplianceItemType
{
    // Employee-level: must have an employeeId
    Iqama,
    Passport,
    WorkPermit,
    HealthInsurance,
    EmploymentContract,

    // Company-level: must NOT have an employeeId
    CommercialRegistration,   // ExpiryDate = next annual confirmation due
    MunicipalLicense,
    CivilDefenseCertificate,

    // Either
    Other
}