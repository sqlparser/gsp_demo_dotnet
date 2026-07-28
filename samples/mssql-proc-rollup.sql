-- Companion to mssql-proc-refresh.sql. This one EXECs the other, so the
-- relations report shows a procedure-to-procedure call and not just table
-- access, and it has no TRY/CATCH so the coverage report reports "No".
--   dotnet run --project src/demos/analyzesp/demos.analyzesp.csproj -c Release -- \
--     samples/mssql-proc-refresh.sql samples/mssql-proc-rollup.sql /t

CREATE PROCEDURE dbo.usp_nightly_rollup
    @as_of DATE
AS
BEGIN
    SET NOCOUNT ON;

    EXEC dbo.usp_refresh_dept_summary;

    UPDATE dbo.dept_salary_summary
    SET    avg_cost = CASE WHEN headcount > 0
                           THEN payroll / headcount
                           ELSE 0
                      END;

    SELECT UPPER(d.department_name) AS department_name,
           s.headcount,
           s.payroll,
           DATEDIFF(day, s.refreshed_at, @as_of) AS staleness_days
    FROM   dbo.dept_salary_summary s
           INNER JOIN dbo.departments d
             ON d.department_id = s.department_id
    ORDER  BY s.payroll DESC;
END;
