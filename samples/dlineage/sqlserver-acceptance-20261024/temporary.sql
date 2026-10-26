CREATE TABLE source_data (id INT, amount DECIMAL(10,2));
CREATE TABLE target_data (id INT, amount DECIMAL(10,2));
SELECT id, amount INTO #stage FROM source_data;
INSERT INTO target_data (id, amount) SELECT id, amount FROM #stage;
