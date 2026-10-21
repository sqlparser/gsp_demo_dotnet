CREATE TABLE source_data (id INT, amount DECIMAL(10,2));
CREATE TABLE target_data (id INT, amount DECIMAL(10,2));
INSERT INTO target_data (id, amount)
SELECT id, amount + 1 FROM source_data WHERE amount > 0;
