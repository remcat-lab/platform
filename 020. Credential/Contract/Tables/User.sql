CREATE TABLE `user` (
    `seq` INT NOT NULL AUTO_INCREMENT,
    `user_id` VARCHAR(50) NOT NULL,
    `user_name` VARCHAR(100) NOT NULL,
    `department_id` VARCHAR(50),
    `department_name` VARCHAR(100),
    `email` VARCHAR(100),
    `mobile` VARCHAR(20),
    PRIMARY KEY (`seq`),
    INDEX `idx_user_id` (`user_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
