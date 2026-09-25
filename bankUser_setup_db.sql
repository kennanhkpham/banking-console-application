create database userBankManagementDB;
use userBankManagementDB;


--NOTE TO SELF : ECECUTE THE USE BANK FILE BEFORE EXECUTING THE SELECT LINE--
CREATE TABLE UserAccounts
(
    accNo INT PRIMARY KEY,
    name VARCHAR(30),
    accBalance INT,
    username VARCHAR(50) NOT NULL,
    password VARCHAR(50) NOT NULL,

    ChequeBookStatus INT NOT NULL
        CONSTRAINT DF_UserAccounts_ChequeBookStatus DEFAULT 0,

    CONSTRAINT CK_UserAccounts_ChequeBookStatus
        CHECK (ChequeBookStatus IN (0, 1, 2))
);

create table Transactions (
    transactionId INT IDENTITY(1,1) PRIMARY KEY,
    accNo INT NOT NULL,
    description VARCHAR(255) NOT NULL,
    createdAt DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (accNo) REFERENCES UserAccounts(accNo) ON DELETE CASCADE
);

INSERT INTO UserAccounts VALUES (42, 'Alice', 250, 'alice1', 'pass1');
INSERT INTO UserAccounts VALUES (87, 'Charlie', 4920, 'penguinz0', 'pass1');
INSERT INTO UserAccounts VALUES (12, 'Diana', 1500, 'princessdiana', 'pass1');
INSERT INTO UserAccounts VALUES (99, 'Ethan', 75, 'ethan1', 'pass1');
INSERT INTO UserAccounts VALUES (33, 'Fiona', 3200, 'flower', 'pass1');
INSERT INTO UserAccounts VALUES (56, 'George', 890, 'bushwacker', 'pass1');
INSERT INTO UserAccounts VALUES (71, 'Hannah', 12500, 'hannamontana', 'pass1');
INSERT INTO UserAccounts VALUES (24, 'Ian', 450, 'ian1', 'pass1');
INSERT INTO UserAccounts VALUES (65, 'Julia', 6100, 'julia1', 'pass1');
INSERT INTO UserAccounts VALUES (18, 'Kevin', 990, 'kevinguyen', 'pass1');


select * from UserAccounts;

ALTER TABLE UserAccounts
ADD ChequeBookStatus INT NOT NULL
    CONSTRAINT DF_UserAccounts_ChequeBookStatus DEFAULT 0;

ALTER TABLE UserAccounts
ADD CONSTRAINT CK_Accounts_ChequeBookStatus
CHECK (ChequeBookStatus IN (0, 1, 2));