-- One stored procedure per file: analyzesp attributes findings per file, so
-- two procedures in one file are both credited to the last one. This procedure
-- wraps its body in TRY/CATCH; its companion deliberately does not, so the
-- try/catch report has both cases to distinguish.
--   dotnet run --project src/demos/analyzesp/demos.analyzesp.csproj -c Release -- \
--     samples/mssql-proc-refresh.sql samples/mssql-proc-rollup.sql /t

CREATE PROCEDURE dbo.usp_refresh_dept_summary
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        DELETE FROM dbo.dept_salary_summary
        WHERE  department_id IS NOT NULL;

        INSERT INTO dbo.dept_salary_summary (department_id, headcount, payroll, refreshed_at)
        SELECT e.department_id,
               COUNT(*),
               SUM(e.salary),
               GETDATE()
        FROM   dbo.employees e
        WHERE  e.salary > 0
        GROUP  BY e.department_id;
    END TRY
    BEGIN CATCH
        INSERT INTO dbo.etl_errors (proc_name, message, logged_at)
        VALUES (ERROR_PROCEDURE(), ERROR_MESSAGE(), GETDATE());
    END CATCH
END;
