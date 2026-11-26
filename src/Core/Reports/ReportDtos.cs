namespace Core.Reports;

public record ReaderLoanCount(string ReaderId, int Total, int Open);
public record OverdueLine(string LoanId, string CopyId, string ReaderId, DateOnly Due, int DaysOverdue);
public record GroupSummary(string Label, int Count);
public record CopyStats(int Books, int Copies, int Issued, double AvgPerBook, int MaxPerBook);

// Додаткове завдання
public record BookAvailabilityLine(string Title, int Copies, int Free);
public record TitleLoanCount(string Isbn, string Title, int Loans);
