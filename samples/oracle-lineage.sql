-- A small ETL chain: staging table -> view -> summary table. Every column in
-- dept_salary_summary traces back through v_employee_costs to employees, which
-- is what the lineage demos are built to show.
--   dotnet run --project src/demos/dlineage/demos.dlineage.csproj -c Release -- /f samples/oracle-lineage.sql /t oracle
--   dotnet run --project src/demos/columnImpact/demos.columnImpact.csproj -c Release -- /f samples/oracle-lineage.sql /t oracle

CREATE TABLE employees (
  employee_id   NUMBER(6)    NOT NULL,
  last_name     VARCHAR2(25) NOT NULL,
  department_id NUMBER(4),
  salary        NUMBER(8,2),
  commission_pct NUMBER(2,2)
);

CREATE VIEW v_employee_costs AS
SELECT e.employee_id,
       e.last_name,
       e.department_id,
       e.salary                                   AS base_salary,
       e.salary * NVL(e.commission_pct, 0)        AS commission,
       e.salary * (1 + NVL(e.commission_pct, 0))  AS total_cost
FROM   employees e;

INSERT INTO dept_salary_summary (department_id, headcount, payroll, avg_cost)
SELECT v.department_id,
       COUNT(*),
       SUM(v.total_cost),
       AVG(v.total_cost)
FROM   v_employee_costs v
WHERE  v.base_salary > 0
GROUP  BY v.department_id;
