-- ============================================================
-- База данных: warehouse_db (MySQL 8.x, XAMPP)
-- Учёт материальных ценностей
-- Запускать в phpMyAdmin: создать БД -> вкладка SQL -> вставить весь скрипт -> Выполнить
-- ============================================================

CREATE DATABASE IF NOT EXISTS warehouse_db
    CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

USE warehouse_db;

-- На всякий случай удалим старые объекты
SET FOREIGN_KEY_CHECKS = 0;
DROP TABLE IF EXISTS audit_log;
DROP TABLE IF EXISTS inventory_checks;
DROP TABLE IF EXISTS movements;
DROP TABLE IF EXISTS items;
DROP TABLE IF EXISTS warehouses;
DROP TABLE IF EXISTS suppliers;
DROP TABLE IF EXISTS categories;
DROP TABLE IF EXISTS users;
DROP TABLE IF EXISTS roles;
DROP VIEW  IF EXISTS v_items_full;
DROP VIEW  IF EXISTS v_warehouse_stats;
DROP PROCEDURE IF EXISTS sp_register_movement;
DROP PROCEDURE IF EXISTS sp_register_user;
DROP PROCEDURE IF EXISTS sp_inventory_check;
SET FOREIGN_KEY_CHECKS = 1;

-- ============================================================
-- 1. Таблицы
-- ============================================================

CREATE TABLE roles (
    role_id     INT          NOT NULL AUTO_INCREMENT,
    role_name   VARCHAR(50)  NOT NULL,
    description VARCHAR(255),
    PRIMARY KEY (role_id),
    UNIQUE KEY uq_role_name (role_name)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE users (
    user_id        INT           NOT NULL AUTO_INCREMENT,
    login          VARCHAR(80)   NOT NULL,
    password_hash  VARCHAR(100)  NOT NULL,
    full_name      VARCHAR(150)  NOT NULL,
    email          VARCHAR(150)  NOT NULL,
    role_id        INT           NOT NULL,
    is_active      TINYINT(1)    NOT NULL DEFAULT 1,
    created_at     DATETIME      NOT NULL DEFAULT NOW(),
    PRIMARY KEY (user_id),
    UNIQUE KEY uq_users_login (login),
    UNIQUE KEY uq_users_email (email),
    CONSTRAINT fk_users_role FOREIGN KEY (role_id) REFERENCES roles (role_id)
        ON DELETE RESTRICT ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE categories (
    category_id INT          NOT NULL AUTO_INCREMENT,
    name        VARCHAR(100) NOT NULL,
    description VARCHAR(255),
    unit        VARCHAR(20)  NOT NULL DEFAULT 'шт.',
    PRIMARY KEY (category_id),
    UNIQUE KEY uq_category_name (name)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE suppliers (
    supplier_id    INT          NOT NULL AUTO_INCREMENT,
    name           VARCHAR(150) NOT NULL,
    contact_person VARCHAR(100),
    phone          VARCHAR(20),
    email          VARCHAR(150),
    inn            VARCHAR(12),
    created_at     DATETIME     NOT NULL DEFAULT NOW(),
    PRIMARY KEY (supplier_id),
    UNIQUE KEY uq_supplier_inn (inn)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE warehouses (
    warehouse_id        INT          NOT NULL AUTO_INCREMENT,
    name                VARCHAR(100) NOT NULL,
    address             VARCHAR(255),
    responsible_user_id INT,
    created_at          DATETIME     NOT NULL DEFAULT NOW(),
    PRIMARY KEY (warehouse_id),
    CONSTRAINT fk_wh_responsible FOREIGN KEY (responsible_user_id)
        REFERENCES users (user_id)
        ON DELETE SET NULL ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE items (
    item_id          INT            NOT NULL AUTO_INCREMENT,
    item_code        VARCHAR(50)    NOT NULL,
    name             VARCHAR(200)   NOT NULL,
    description      TEXT,
    category_id      INT            NOT NULL,
    unit_price       DECIMAL(10,2)  NOT NULL DEFAULT 0.00,
    min_quantity     INT            NOT NULL DEFAULT 0,
    current_quantity INT            NOT NULL DEFAULT 0,
    warehouse_id     INT            NOT NULL,
    created_at       DATETIME       NOT NULL DEFAULT NOW(),
    updated_at       DATETIME,
    PRIMARY KEY (item_id),
    UNIQUE KEY uq_item_code (item_code),
    CONSTRAINT fk_items_category FOREIGN KEY (category_id)
        REFERENCES categories (category_id) ON DELETE RESTRICT ON UPDATE CASCADE,
    CONSTRAINT fk_items_warehouse FOREIGN KEY (warehouse_id)
        REFERENCES warehouses (warehouse_id) ON DELETE RESTRICT ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE movements (
    movement_id     INT            NOT NULL AUTO_INCREMENT,
    item_id         INT            NOT NULL,
    movement_type   VARCHAR(20)    NOT NULL,
    quantity        INT            NOT NULL,
    unit_price      DECIMAL(10,2)  NOT NULL,
    total_amount    DECIMAL(12,2)  NOT NULL,
    supplier_id     INT,
    warehouse_id    INT            NOT NULL,
    created_by      INT            NOT NULL,
    document_number VARCHAR(50),
    notes           TEXT,
    movement_date   DATETIME       NOT NULL DEFAULT NOW(),
    PRIMARY KEY (movement_id),
    CONSTRAINT fk_mv_item       FOREIGN KEY (item_id)      REFERENCES items (item_id)         ON DELETE CASCADE   ON UPDATE CASCADE,
    CONSTRAINT fk_mv_supplier   FOREIGN KEY (supplier_id)  REFERENCES suppliers (supplier_id) ON DELETE SET NULL  ON UPDATE CASCADE,
    CONSTRAINT fk_mv_warehouse  FOREIGN KEY (warehouse_id) REFERENCES warehouses (warehouse_id) ON DELETE RESTRICT ON UPDATE CASCADE,
    CONSTRAINT fk_mv_created_by FOREIGN KEY (created_by)   REFERENCES users (user_id)         ON DELETE RESTRICT  ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE inventory_checks (
    check_id          INT       NOT NULL AUTO_INCREMENT,
    item_id           INT       NOT NULL,
    expected_quantity INT       NOT NULL,
    actual_quantity   INT       NOT NULL,
    difference        INT       NOT NULL,
    checked_by        INT       NOT NULL,
    check_date        DATETIME  NOT NULL DEFAULT NOW(),
    notes             TEXT,
    PRIMARY KEY (check_id),
    CONSTRAINT fk_ic_item FOREIGN KEY (item_id)    REFERENCES items (item_id) ON DELETE CASCADE  ON UPDATE CASCADE,
    CONSTRAINT fk_ic_user FOREIGN KEY (checked_by) REFERENCES users (user_id) ON DELETE RESTRICT ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE audit_log (
    log_id        INT         NOT NULL AUTO_INCREMENT,
    item_id       INT         NOT NULL,
    changed_by    INT,
    action        VARCHAR(20) NOT NULL,
    old_quantity  INT,
    new_quantity  INT,
    changed_at    DATETIME    NOT NULL DEFAULT NOW(),
    PRIMARY KEY (log_id),
    CONSTRAINT fk_log_item FOREIGN KEY (item_id)    REFERENCES items (item_id) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT fk_log_user FOREIGN KEY (changed_by) REFERENCES users (user_id) ON DELETE SET NULL ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Индексы
CREATE INDEX idx_items_category   ON items (category_id);
CREATE INDEX idx_items_warehouse  ON items (warehouse_id);
CREATE INDEX idx_movements_item   ON movements (item_id);
CREATE INDEX idx_movements_date   ON movements (movement_date);
CREATE INDEX idx_movements_type   ON movements (movement_type);

-- ============================================================
-- 2. Тестовые данные
-- ============================================================

INSERT INTO roles (role_name, description) VALUES
    ('admin',       'Администратор системы'),
    ('storekeeper', 'Кладовщик — приход, расход, инвентаризация'),
    ('accountant',  'Бухгалтер — отчёты и стоимостной учёт');

-- Пароли захэшированы BCrypt (work factor 11)
-- admin        -> admin123
-- storekeeper1 -> store123
-- accountant1  -> acc12345
INSERT INTO users (login, password_hash, full_name, email, role_id, is_active) VALUES
    ('admin',
     '$2b$11$EPROkVOqZzuj50XlfRltju6cZeS0js6PhRoj2e1BKuNpqfxkojP32',
     'Иванов Иван Иванович', 'admin@warehouse.ru',
     (SELECT role_id FROM roles WHERE role_name='admin'), 1),
    ('storekeeper1',
     '$2b$11$MS74AgiD7MwEv10.69PG0.Nh9i5OmwlmHvFsVwheJzfet94tyuZwq',
     'Петров Пётр Петрович', 'petrov@warehouse.ru',
     (SELECT role_id FROM roles WHERE role_name='storekeeper'), 1),
    ('accountant1',
     '$2b$11$7vMgdSwxf87vhAKeDaRfv.cxKynlAkTK.CA4owEg5gfjWfmvYoPN2',
     'Сидорова Анна Викторовна', 'sidorova@warehouse.ru',
     (SELECT role_id FROM roles WHERE role_name='accountant'), 1);

INSERT INTO categories (name, description, unit) VALUES
    ('Канцелярия',     'Бумага, ручки, скрепки и т.п.',   'шт.'),
    ('Стройматериалы', 'Цемент, краска, инструмент',      'кг'),
    ('Электроника',    'Компьютерная техника, кабели',    'шт.'),
    ('Спецодежда',     'Рабочая одежда, СИЗ',             'шт.'),
    ('Расходники',     'Картриджи, бумага для принтеров', 'шт.');

INSERT INTO suppliers (name, contact_person, phone, email, inn) VALUES
    ('ООО "Канцторг"',   'Смирнов А.В.',  '+7-495-111-22-33', 'sales@kancmarket.ru', '7701234567'),
    ('ИП Иванов И.И.',   'Иванов И.И.',   '+7-916-555-44-33', 'ivanov@email.ru',     '772212345678'),
    ('ООО "СтройСнаб"',  'Кузнецов В.П.', '+7-495-987-65-43', 'snab@stroy.ru',       '7705678901'),
    ('ООО "Электроком"', 'Лебедев С.Н.',  '+7-495-321-12-34', 'order@elcom.ru',      '7708765432');

INSERT INTO warehouses (name, address, responsible_user_id) VALUES
    ('Главный склад',     'г. Москва, ул. Ленина, д. 1',
     (SELECT user_id FROM users WHERE login='storekeeper1')),
    ('Склад канцтоваров', 'г. Москва, ул. Пушкина, д. 5',
     (SELECT user_id FROM users WHERE login='storekeeper1')),
    ('Склад техники',     'г. Москва, ул. Гагарина, д. 10',
     (SELECT user_id FROM users WHERE login='storekeeper1'));

INSERT INTO items (item_code, name, description, category_id, unit_price, min_quantity, current_quantity, warehouse_id) VALUES
    ('K-001', 'Бумага А4 SvetoCopy 500л', 'Офисная бумага плотностью 80 г/м²', 1, 320.00, 50,  200, 2),
    ('K-002', 'Ручка шариковая синяя',    'Pilot BPS-GP-F',                    1,  45.00, 100, 350, 2),
    ('K-003', 'Папка-скоросшиватель',     'Папка А4 пластиковая',              1,  25.00, 50,  120, 2),
    ('E-001', 'Картридж HP CF283A',       'Совместимый, чёрный',               5, 1850.00, 5,   12, 3),
    ('E-002', 'Кабель HDMI 2 м',          'HDMI 2.0, поддержка 4K',            3,  450.00, 10,  25, 3),
    ('S-001', 'Цемент М400 50 кг',        'Портландцемент',                    2,  380.00, 20,  85, 1),
    ('SP-001','Перчатки рабочие',         'Хлопок с ПВХ-точкой',               4,   35.00, 100, 240, 1);

INSERT INTO movements (item_id, movement_type, quantity, unit_price, total_amount, supplier_id, warehouse_id, created_by, document_number, notes) VALUES
    (1, 'in',  100, 320.00, 32000.00, 1, 2,
     (SELECT user_id FROM users WHERE login='storekeeper1'),
     'ТН-2025-001', 'Поступление от Канцторг'),
    (2, 'in',  200,  45.00,  9000.00, 1, 2,
     (SELECT user_id FROM users WHERE login='storekeeper1'),
     'ТН-2025-002', 'Поступление от Канцторг'),
    (4, 'in',   10, 1850.00, 18500.00, 4, 3,
     (SELECT user_id FROM users WHERE login='storekeeper1'),
     'ТН-2025-003', 'Поступление картриджей'),
    (1, 'out',  30, 320.00,  9600.00, NULL, 2,
     (SELECT user_id FROM users WHERE login='storekeeper1'),
     'РАС-2025-015', 'Выдача в бухгалтерию'),
    (6, 'in',   50, 380.00, 19000.00, 3, 1,
     (SELECT user_id FROM users WHERE login='storekeeper1'),
     'ТН-2025-004', 'Поступление цемента');

INSERT INTO inventory_checks (item_id, expected_quantity, actual_quantity, difference, checked_by, notes) VALUES
    (1, 200, 198, -2, (SELECT user_id FROM users WHERE login='storekeeper1'), 'Незначительное расхождение'),
    (2, 350, 350,  0, (SELECT user_id FROM users WHERE login='storekeeper1'), 'Расхождений нет'),
    (4,  12,  11, -1, (SELECT user_id FROM users WHERE login='storekeeper1'), 'Один картридж повреждён');

-- ============================================================
-- 3. VIEW
-- ============================================================

CREATE OR REPLACE VIEW v_items_full AS
SELECT
    i.item_id,
    i.item_code,
    i.name        AS item_name,
    i.category_id,
    c.name        AS category,
    c.unit,
    i.unit_price,
    i.current_quantity,
    i.min_quantity,
    CASE
        WHEN i.current_quantity = 0                 THEN 'Отсутствует'
        WHEN i.current_quantity <= i.min_quantity   THEN 'Низкий остаток'
        ELSE                                              'В наличии'
    END AS stock_status,
    i.warehouse_id,
    w.name        AS warehouse,
    i.created_at,
    i.updated_at
FROM items i
JOIN categories c ON i.category_id  = c.category_id
JOIN warehouses w ON i.warehouse_id = w.warehouse_id;

CREATE OR REPLACE VIEW v_warehouse_stats AS
SELECT
    w.warehouse_id,
    w.name AS warehouse_name,
    u.full_name AS responsible,
    COUNT(i.item_id) AS items_count,
    COALESCE(SUM(i.current_quantity), 0) AS total_quantity,
    COALESCE(SUM(i.current_quantity * i.unit_price), 0) AS total_value,
    COUNT(CASE WHEN i.current_quantity <= i.min_quantity THEN 1 END) AS low_stock_count
FROM warehouses w
LEFT JOIN items i ON w.warehouse_id      = i.warehouse_id
LEFT JOIN users u ON w.responsible_user_id = u.user_id
GROUP BY w.warehouse_id, w.name, u.full_name;

-- ============================================================
-- 4. Хранимые процедуры
-- ============================================================

DELIMITER $$

CREATE PROCEDURE sp_register_movement(
    IN p_item_id       INT,
    IN p_type          VARCHAR(20),
    IN p_quantity      INT,
    IN p_unit_price    DECIMAL(10,2),
    IN p_supplier_id   INT,
    IN p_warehouse_id  INT,
    IN p_user_id       INT,
    IN p_doc_number    VARCHAR(50),
    IN p_notes         TEXT
)
BEGIN
    DECLARE v_current INT;

    SELECT current_quantity INTO v_current FROM items WHERE item_id = p_item_id;

    IF p_type = 'out' AND v_current < p_quantity THEN
        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'Недостаточно материала на складе для расхода';
    END IF;

    INSERT INTO movements
        (item_id, movement_type, quantity, unit_price, total_amount,
         supplier_id, warehouse_id, created_by, document_number, notes, movement_date)
    VALUES
        (p_item_id, p_type, p_quantity, p_unit_price, p_quantity * p_unit_price,
         p_supplier_id, p_warehouse_id, p_user_id, p_doc_number, p_notes, NOW());

    IF p_type = 'in' THEN
        UPDATE items SET current_quantity = current_quantity + p_quantity
         WHERE item_id = p_item_id;
    ELSE
        UPDATE items SET current_quantity = current_quantity - p_quantity
         WHERE item_id = p_item_id;
    END IF;
END$$

CREATE PROCEDURE sp_register_user(
    IN p_login    VARCHAR(80),
    IN p_hash     VARCHAR(100),
    IN p_fullname VARCHAR(150),
    IN p_email    VARCHAR(150),
    IN p_role_id  INT
)
BEGIN
    IF EXISTS (SELECT 1 FROM users WHERE login = p_login) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Пользователь с таким логином уже существует';
    END IF;
    IF EXISTS (SELECT 1 FROM users WHERE email = p_email) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Этот email уже зарегистрирован';
    END IF;
    INSERT INTO users (login, password_hash, full_name, email, role_id, is_active, created_at)
    VALUES (p_login, p_hash, p_fullname, p_email, p_role_id, 1, NOW());
END$$

CREATE PROCEDURE sp_inventory_check(
    IN p_item_id          INT,
    IN p_actual_quantity  INT,
    IN p_user_id          INT,
    IN p_notes            TEXT
)
BEGIN
    DECLARE v_expected INT;
    DECLARE v_diff     INT;

    SELECT current_quantity INTO v_expected FROM items WHERE item_id = p_item_id;
    SET v_diff = p_actual_quantity - v_expected;

    INSERT INTO inventory_checks (item_id, expected_quantity, actual_quantity, difference, checked_by, check_date, notes)
    VALUES (p_item_id, v_expected, p_actual_quantity, v_diff, p_user_id, NOW(), p_notes);

    IF v_diff <> 0 THEN
        UPDATE items SET current_quantity = p_actual_quantity WHERE item_id = p_item_id;
    END IF;
END$$

DELIMITER ;

-- ============================================================
-- 5. Триггеры
-- ============================================================

DELIMITER $$

CREATE TRIGGER trg_audit_quantity
AFTER UPDATE ON items
FOR EACH ROW
BEGIN
    IF OLD.current_quantity <> NEW.current_quantity THEN
        INSERT INTO audit_log (item_id, changed_by, action, old_quantity, new_quantity, changed_at)
        VALUES (NEW.item_id, NULL, 'UPDATE', OLD.current_quantity, NEW.current_quantity, NOW());
    END IF;
END$$

CREATE TRIGGER trg_items_updated_at
BEFORE UPDATE ON items
FOR EACH ROW
BEGIN
    SET NEW.updated_at = NOW();
END$$

DELIMITER ;

-- ============================================================
-- 6. Роли СУБД (опционально - выполнить если есть права)
-- ============================================================

-- CREATE ROLE IF NOT EXISTS 'wh_readonly';
-- CREATE ROLE IF NOT EXISTS 'wh_operator';
-- CREATE ROLE IF NOT EXISTS 'wh_admin';
--
-- GRANT SELECT ON warehouse_db.* TO 'wh_readonly';
-- GRANT SELECT, INSERT, UPDATE ON warehouse_db.items     TO 'wh_operator';
-- GRANT SELECT, INSERT          ON warehouse_db.movements TO 'wh_operator';
-- GRANT EXECUTE ON PROCEDURE warehouse_db.sp_register_movement TO 'wh_operator';
-- GRANT ALL PRIVILEGES ON warehouse_db.* TO 'wh_admin';

-- Готово. Проверка:
SELECT 'База warehouse_db готова. Тестовые учётки:' AS message
UNION ALL SELECT 'admin / admin123 (Администратор)'
UNION ALL SELECT 'storekeeper1 / store123 (Кладовщик)'
UNION ALL SELECT 'accountant1 / acc12345 (Бухгалтер)';
