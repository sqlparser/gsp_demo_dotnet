CREATE TABLE source_data (id NUMBER, amount NUMBER);
CREATE TABLE target_data (amount NUMBER);
CREATE SYNONYM source_alias FOR source_data;
DECLARE
    v_amount NUMBER;
    CURSOR amounts IS SELECT amount FROM source_alias;
BEGIN
    SELECT amount INTO v_amount FROM source_alias WHERE id = 1;
    INSERT INTO target_data (amount) VALUES (v_amount);
END;
/
