INSERT INTO employee_names (name)
SELECT ename FROM emp, dept WHERE emp.deptid = dept.id;
