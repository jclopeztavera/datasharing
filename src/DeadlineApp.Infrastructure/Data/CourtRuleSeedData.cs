using DeadlineApp.Core.Entities;
using DeadlineApp.Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace DeadlineApp.Infrastructure.Data;

/// <summary>
/// Seed data for Florida and New York family law court rules.
/// Citations reference the Florida Family Law Rules of Procedure (Fla. Fam. L. R. P.)
/// and New York Civil Practice Law and Rules (CPLR) / 22 NYCRR 202.16.
///
/// DISCLAIMER: These rules are for deadline calculation purposes only
/// and do not constitute legal advice. Rules are subject to change.
/// Always verify against current statutes and court orders.
/// </summary>
public static class CourtRuleSeedData
{
    /// <summary>
    /// Seeds court rules into the database if none exist.
    /// Idempotent — will not duplicate rules on subsequent calls.
    /// </summary>
    public static async Task SeedAsync(DeadlineDbContext context)
    {
        if (await context.CourtRules.AnyAsync())
        {
            return; // Already seeded
        }

        var rules = GetAllRules();
        context.CourtRules.AddRange(rules);
        await context.SaveChangesAsync();
    }

    private static readonly DateTime EffectiveDate = new(2020, 1, 1);

    private static readonly string[] AllFamilyCaseTypes =
    {
        "Divorce", "Custody", "Child Support", "Alimony", "Paternity", "Adoption"
    };

    // Case types that require mandatory financial disclosure
    private static readonly string[] FinancialCaseTypes =
    {
        "Divorce", "Child Support", "Alimony", "Paternity"
    };

    // Case types where discovery applies broadly
    private static readonly string[] DiscoveryCaseTypes =
    {
        "Divorce", "Custody", "Child Support", "Alimony", "Paternity"
    };

    public static IReadOnlyList<CourtRule> GetAllRules()
    {
        var rules = new List<CourtRule>();
        rules.AddRange(GetFloridaRules());
        rules.AddRange(GetNewYorkRules());
        return rules.AsReadOnly();
    }

    public static IReadOnlyList<CourtRule> GetFloridaRules()
    {
        var rules = new List<CourtRule>();
        rules.AddRange(GetFloridaResponseRules());
        rules.AddRange(GetFloridaDiscoveryRules());
        rules.AddRange(GetFloridaFinancialDisclosureRules());
        rules.AddRange(GetFloridaHearingRules());
        return rules.AsReadOnly();
    }

    public static IReadOnlyList<CourtRule> GetNewYorkRules()
    {
        var rules = new List<CourtRule>();
        rules.AddRange(GetNewYorkResponseRules());
        rules.AddRange(GetNewYorkDiscoveryRules());
        rules.AddRange(GetNewYorkFinancialDisclosureRules());
        rules.AddRange(GetNewYorkHearingRules());
        return rules.AsReadOnly();
    }

    // =====================================================================
    // FLORIDA RULES
    // =====================================================================

    #region Florida Response Deadlines

    private static IEnumerable<CourtRule> GetFloridaResponseRules()
    {
        // Fla. Fam. L. R. P. 12.140 — Defenses
        // Response due within 20 days after service of process and initial pleading
        foreach (var caseType in AllFamilyCaseTypes)
        {
            yield return CreateRule(
                state: "FL",
                caseType: caseType,
                ruleName: "Response to Petition",
                ruleDescription:
                    "Respondent must serve a response within 20 days after service of " +
                    "original process and the initial pleading. Failure to respond may " +
                    "result in a default under Rule 12.500.",
                ruleCitation: "Fla. Fam. L. R. P. 12.140(a)",
                triggerType: TriggerEventType.ServiceDate,
                daysFromTrigger: 20,
                countBusinessDays: false,
                bufferDays: 5);

            yield return CreateRule(
                state: "FL",
                caseType: caseType,
                ruleName: "Response to Counterpetition",
                ruleDescription:
                    "Petitioner must serve a response to a counterpetition within 20 days " +
                    "after service of the counterpetition.",
                ruleCitation: "Fla. Fam. L. R. P. 12.140(a)",
                triggerType: TriggerEventType.ServiceDate,
                daysFromTrigger: 20,
                countBusinessDays: false,
                bufferDays: 5);
        }
    }

    #endregion

    #region Florida Discovery Deadlines

    private static IEnumerable<CourtRule> GetFloridaDiscoveryRules()
    {
        foreach (var caseType in DiscoveryCaseTypes)
        {
            // Fla. Fam. L. R. P. 12.340 — Interrogatories
            // Standard response: 30 days after service
            yield return CreateRule(
                state: "FL",
                caseType: caseType,
                ruleName: "Response to Interrogatories",
                ruleDescription:
                    "Answers to interrogatories must be served within 30 days after " +
                    "service of the interrogatories. Leave of court required to exceed " +
                    "10 additional interrogatories beyond standard form.",
                ruleCitation: "Fla. Fam. L. R. P. 12.340(a)",
                triggerType: TriggerEventType.ServiceDate,
                daysFromTrigger: 30,
                countBusinessDays: false,
                bufferDays: 7);

            // Fla. Fam. L. R. P. 12.340 — Interrogatories served with process
            // Response: 45 days when served with initial process
            yield return CreateRule(
                state: "FL",
                caseType: caseType,
                ruleName: "Response to Interrogatories (Served with Process)",
                ruleDescription:
                    "When interrogatories are served with the initial process, the " +
                    "respondent has 45 days to respond instead of the standard 30 days.",
                ruleCitation: "Fla. Fam. L. R. P. 12.340(a)",
                triggerType: TriggerEventType.ServiceDate,
                daysFromTrigger: 45,
                countBusinessDays: false,
                bufferDays: 7);

            // Fla. Fam. L. R. P. 12.350 — Request for Production
            // Standard response: 30 days after service
            yield return CreateRule(
                state: "FL",
                caseType: caseType,
                ruleName: "Response to Request for Production",
                ruleDescription:
                    "Written response to requests for production of documents must be " +
                    "served within 30 days after service of the request.",
                ruleCitation: "Fla. Fam. L. R. P. 12.350(b)",
                triggerType: TriggerEventType.ServiceDate,
                daysFromTrigger: 30,
                countBusinessDays: false,
                bufferDays: 7);

            // Fla. Fam. L. R. P. 12.350 — Request for Production served with process
            yield return CreateRule(
                state: "FL",
                caseType: caseType,
                ruleName: "Response to Request for Production (Served with Process)",
                ruleDescription:
                    "When requests for production are served with initial process, " +
                    "the respondent has 45 days to respond.",
                ruleCitation: "Fla. Fam. L. R. P. 12.350(b)",
                triggerType: TriggerEventType.ServiceDate,
                daysFromTrigger: 45,
                countBusinessDays: false,
                bufferDays: 7);

            // Fla. Fam. L. R. P. 12.370 / Fla. R. Civ. P. 1.370 — Requests for Admission
            // Standard response: 30 days after service; deemed admitted if no response
            yield return CreateRule(
                state: "FL",
                caseType: caseType,
                ruleName: "Response to Requests for Admission",
                ruleDescription:
                    "Responses to requests for admission must be served within 30 days " +
                    "after service. Matters are deemed admitted if not responded to " +
                    "within the deadline. Limit of 30 requests including subparts.",
                ruleCitation: "Fla. Fam. L. R. P. 12.370; Fla. R. Civ. P. 1.370",
                triggerType: TriggerEventType.ServiceDate,
                daysFromTrigger: 30,
                countBusinessDays: false,
                bufferDays: 5);

            // Requests for Admission served with process
            yield return CreateRule(
                state: "FL",
                caseType: caseType,
                ruleName: "Response to Requests for Admission (Served with Process)",
                ruleDescription:
                    "When requests for admission are served with initial process, " +
                    "the respondent has 45 days to respond. Matters are deemed " +
                    "admitted if not timely answered.",
                ruleCitation: "Fla. Fam. L. R. P. 12.370; Fla. R. Civ. P. 1.370",
                triggerType: TriggerEventType.ServiceDate,
                daysFromTrigger: 45,
                countBusinessDays: false,
                bufferDays: 5);
        }
    }

    #endregion

    #region Florida Financial Disclosure Deadlines

    private static IEnumerable<CourtRule> GetFloridaFinancialDisclosureRules()
    {
        foreach (var caseType in FinancialCaseTypes)
        {
            // Fla. Fam. L. R. P. 12.285 — Mandatory Disclosure
            // 45 days after service of initial pleading
            yield return CreateRule(
                state: "FL",
                caseType: caseType,
                ruleName: "Mandatory Financial Disclosure",
                ruleDescription:
                    "Each party must serve mandatory financial disclosure within " +
                    "45 days after service of the initial pleading on the respondent. " +
                    "Includes Financial Affidavit (short form if income < $50,000, " +
                    "long form if >= $50,000), tax returns, pay stubs, bank statements, " +
                    "and other financial documents per Rule 12.285(e).",
                ruleCitation: "Fla. Fam. L. R. P. 12.285(b)",
                triggerType: TriggerEventType.ServiceDate,
                daysFromTrigger: 45,
                countBusinessDays: false,
                bufferDays: 10);
        }

        // Additional hearing-based financial disclosure rules for all financial cases
        foreach (var caseType in FinancialCaseTypes)
        {
            // Movant must serve financial documents 10 days before temp hearing
            yield return CreateRule(
                state: "FL",
                caseType: caseType,
                ruleName: "Movant Financial Docs for Temporary Hearing",
                ruleDescription:
                    "Party seeking temporary financial relief must serve required " +
                    "financial documents 10 days before the temporary hearing.",
                ruleCitation: "Fla. Fam. L. R. P. 12.285(d)",
                triggerType: TriggerEventType.HearingDate,
                daysFromTrigger: 10,
                countBusinessDays: false,
                bufferDays: 3);

            // Respondent documents due 7 days before temp hearing (by mail)
            yield return CreateRule(
                state: "FL",
                caseType: caseType,
                ruleName: "Respondent Financial Docs for Temporary Hearing (Mail)",
                ruleDescription:
                    "Responding party must mail required financial documents to the " +
                    "party seeking temporary financial relief at least 7 days before " +
                    "the hearing, or hand-deliver 2 business days before (by 5:00 PM).",
                ruleCitation: "Fla. Fam. L. R. P. 12.285(d)",
                triggerType: TriggerEventType.HearingDate,
                daysFromTrigger: 7,
                countBusinessDays: false,
                bufferDays: 2);
        }
    }

    #endregion

    #region Florida Hearing-Related Deadlines

    private static IEnumerable<CourtRule> GetFloridaHearingRules()
    {
        foreach (var caseType in AllFamilyCaseTypes)
        {
            // Fla. R. Jud. Admin. 2.516 / Fla. Fam. L. R. P. 12.090
            // Notice of hearing: at least 5 days before hearing
            yield return CreateRule(
                state: "FL",
                caseType: caseType,
                ruleName: "Notice of Hearing on Motion",
                ruleDescription:
                    "A copy of any written motion and notice of hearing must be " +
                    "served a reasonable time before the hearing, generally at least " +
                    "5 days before the hearing date.",
                ruleCitation: "Fla. Fam. L. R. P. 12.090; Fla. R. Jud. Admin. 2.516",
                triggerType: TriggerEventType.HearingDate,
                daysFromTrigger: 5,
                countBusinessDays: false,
                bufferDays: 2);
        }
    }

    #endregion

    // =====================================================================
    // NEW YORK RULES
    // =====================================================================

    #region New York Response Deadlines

    private static IEnumerable<CourtRule> GetNewYorkResponseRules()
    {
        foreach (var caseType in AllFamilyCaseTypes)
        {
            // CPLR 3012(a) — Personal service within NY: 20 days
            yield return CreateRule(
                state: "NY",
                caseType: caseType,
                ruleName: "Answer to Complaint (Personal Service in NY)",
                ruleDescription:
                    "Defendant must serve an answer within 20 days after personal " +
                    "service of the summons and complaint within New York State.",
                ruleCitation: "CPLR § 3012(a)",
                triggerType: TriggerEventType.ServiceDate,
                daysFromTrigger: 20,
                countBusinessDays: false,
                bufferDays: 5);

            // CPLR 3012(a) — Other service methods: 30 days
            yield return CreateRule(
                state: "NY",
                caseType: caseType,
                ruleName: "Answer to Complaint (Substituted/Other Service)",
                ruleDescription:
                    "Defendant must serve an answer within 30 days after service is " +
                    "complete when served by substituted service, conspicuous place " +
                    "service, or other methods under CPLR 308(2)-(5), 313, 314, or 315.",
                ruleCitation: "CPLR § 3012(a)",
                triggerType: TriggerEventType.ServiceDate,
                daysFromTrigger: 30,
                countBusinessDays: false,
                bufferDays: 5);

            // CPLR 3012(b) — Demand for complaint after summons with notice
            yield return CreateRule(
                state: "NY",
                caseType: caseType,
                ruleName: "Plaintiff's Complaint After Demand",
                ruleDescription:
                    "After defendant serves a notice of appearance and demand for " +
                    "complaint, plaintiff must serve the complaint within 20 days.",
                ruleCitation: "CPLR § 3012(b)",
                triggerType: TriggerEventType.ServiceDate,
                daysFromTrigger: 20,
                countBusinessDays: false,
                bufferDays: 5);
        }
    }

    #endregion

    #region New York Discovery Deadlines

    private static IEnumerable<CourtRule> GetNewYorkDiscoveryRules()
    {
        foreach (var caseType in DiscoveryCaseTypes)
        {
            // CPLR 3133 — Interrogatories: 20 days to respond
            yield return CreateRule(
                state: "NY",
                caseType: caseType,
                ruleName: "Response to Interrogatories",
                ruleDescription:
                    "Responses to interrogatories must be served within 20 days after " +
                    "service of the interrogatories. In matrimonial actions, " +
                    "interrogatories are limited to 25 including subparts.",
                ruleCitation: "CPLR § 3133(a); 22 NYCRR § 202.16(f)(3)",
                triggerType: TriggerEventType.ServiceDate,
                daysFromTrigger: 20,
                countBusinessDays: false,
                bufferDays: 5);

            // CPLR 3120 — Document production: 20 days to respond
            yield return CreateRule(
                state: "NY",
                caseType: caseType,
                ruleName: "Response to Document Production Request",
                ruleDescription:
                    "Written response to a notice to produce documents must be " +
                    "served within 20 days after service of the notice. " +
                    "Five additional days if served by mail.",
                ruleCitation: "CPLR § 3120(2); CPLR § 2103(b)(2)",
                triggerType: TriggerEventType.ServiceDate,
                daysFromTrigger: 20,
                countBusinessDays: false,
                bufferDays: 5);

            // CPLR 3123 — Requests for admission: 20 days
            yield return CreateRule(
                state: "NY",
                caseType: caseType,
                ruleName: "Response to Requests for Admission",
                ruleDescription:
                    "Responses to requests for admission must be served within 20 days. " +
                    "Matters are deemed admitted if no written sworn response is served " +
                    "within the deadline. Five additional days if served by mail.",
                ruleCitation: "CPLR § 3123(a)",
                triggerType: TriggerEventType.ServiceDate,
                daysFromTrigger: 20,
                countBusinessDays: false,
                bufferDays: 5);

            // CPLR 3122 — Objections to disclosure: 20 days
            yield return CreateRule(
                state: "NY",
                caseType: caseType,
                ruleName: "Objections to Disclosure Demand",
                ruleDescription:
                    "Objections to a disclosure demand must be served within 20 days " +
                    "of service of the demand.",
                ruleCitation: "CPLR § 3122",
                triggerType: TriggerEventType.ServiceDate,
                daysFromTrigger: 20,
                countBusinessDays: false,
                bufferDays: 5);
        }
    }

    #endregion

    #region New York Financial Disclosure Deadlines

    private static IEnumerable<CourtRule> GetNewYorkFinancialDisclosureRules()
    {
        foreach (var caseType in FinancialCaseTypes)
        {
            // 22 NYCRR 202.16(f) — Expert report exchange: 60 days before trial
            yield return CreateRule(
                state: "NY",
                caseType: caseType,
                ruleName: "Expert Witness Report Exchange",
                ruleDescription:
                    "Each expert witness whom a party expects to call at trial must " +
                    "file a written report, exchanged and filed no later than 60 days " +
                    "before the date set for trial.",
                ruleCitation: "22 NYCRR § 202.16(g)",
                triggerType: TriggerEventType.HearingDate,
                daysFromTrigger: 60,
                countBusinessDays: false,
                bufferDays: 14);

            // 22 NYCRR 202.16(g) — Reply expert reports: 30 days before trial
            yield return CreateRule(
                state: "NY",
                caseType: caseType,
                ruleName: "Reply Expert Witness Report",
                ruleDescription:
                    "Reply expert reports must be exchanged and filed no later than " +
                    "30 days before the date set for trial.",
                ruleCitation: "22 NYCRR § 202.16(g)",
                triggerType: TriggerEventType.HearingDate,
                daysFromTrigger: 30,
                countBusinessDays: false,
                bufferDays: 7);

            // 22 NYCRR 202.16(f) — Expert demand response: 20 days
            yield return CreateRule(
                state: "NY",
                caseType: caseType,
                ruleName: "Response to Expert Information Demand",
                ruleDescription:
                    "Responses to demands for expert information pursuant to " +
                    "CPLR 3101(d) must be served within 20 days following " +
                    "service of such demands.",
                ruleCitation: "22 NYCRR § 202.16(f)(1); CPLR § 3101(d)",
                triggerType: TriggerEventType.ServiceDate,
                daysFromTrigger: 20,
                countBusinessDays: false,
                bufferDays: 5);
        }
    }

    #endregion

    #region New York Hearing-Related Deadlines

    private static IEnumerable<CourtRule> GetNewYorkHearingRules()
    {
        foreach (var caseType in AllFamilyCaseTypes)
        {
            // CPLR 2214(b) — Motion notice: 8 days before return date
            yield return CreateRule(
                state: "NY",
                caseType: caseType,
                ruleName: "Notice of Motion",
                ruleDescription:
                    "A notice of motion must be served at least 8 days before the " +
                    "return date. Add 5 days if served by mail. Answering papers " +
                    "are due 2 days before the return date (or 7 days before if " +
                    "motion served 16+ days in advance).",
                ruleCitation: "CPLR § 2214(b)",
                triggerType: TriggerEventType.HearingDate,
                daysFromTrigger: 8,
                countBusinessDays: false,
                bufferDays: 3);

            // CPLR 2214(b) — Answering papers for standard motion
            yield return CreateRule(
                state: "NY",
                caseType: caseType,
                ruleName: "Answering Papers on Motion",
                ruleDescription:
                    "Answering papers or a cross-motion must be served no later " +
                    "than 2 days before the return date of the motion (or 7 days " +
                    "before if the motion was served 16+ days in advance).",
                ruleCitation: "CPLR § 2214(b)",
                triggerType: TriggerEventType.HearingDate,
                daysFromTrigger: 2,
                countBusinessDays: false,
                bufferDays: 1);
        }
    }

    #endregion

    // =====================================================================
    // HELPER
    // =====================================================================

    private static CourtRule CreateRule(
        string state,
        string caseType,
        string ruleName,
        string ruleDescription,
        string ruleCitation,
        TriggerEventType triggerType,
        int daysFromTrigger,
        bool countBusinessDays,
        int? bufferDays,
        string? county = null)
    {
        return new CourtRule
        {
            Id = Guid.NewGuid(),
            State = state,
            County = county,
            CaseType = caseType,
            RuleName = ruleName,
            RuleDescription = ruleDescription,
            RuleCitation = ruleCitation,
            TriggerEventType = triggerType,
            DaysFromTrigger = daysFromTrigger,
            CountBusinessDays = countBusinessDays,
            ExcludeHolidays = true,
            BufferDays = bufferDays,
            IsActive = true,
            EffectiveDate = EffectiveDate,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "system-seed"
        };
    }
}
