CREATE TABLE employees (id NUMBER, dept_id NUMBER, salary NUMBER);
CREATE TABLE departments (id NUMBER, name VARCHAR2(50));
CREATE TABLE salary_report (dept_name VARCHAR2(50), total NUMBER, staff NUMBER);
CREATE VIEW employee_costs AS
SELECT e.id, d.name AS dept_name,
       CASE WHEN e.salary > 1000 THEN e.salary * 1.1 ELSE e.salary END AS cost
FROM employees e LEFT JOIN departments d ON e.dept_id = d.id;
INSERT INTO salary_report (dept_name, total, staff)
WITH costs AS (SELECT dept_name, cost FROM employee_costs)
SELECT dept_name, SUM(cost), COUNT(cost) FROM costs GROUP BY dept_name;
