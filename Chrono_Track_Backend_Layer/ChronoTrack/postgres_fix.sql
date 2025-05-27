-- PostgreSQL fix script for Chrono Track
-- This script addresses common issues in the containerized PostgreSQL database

-- 1. Fix shadow properties in Users table
DO $$
BEGIN
    -- Check if both DepartmentID and DepartmentId exist
    IF EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_schema = 'app' 
        AND table_name = 'Users' 
        AND column_name = 'DepartmentId'
    ) THEN
        -- Copy data from shadow property to main property if needed
        UPDATE app."Users" 
        SET "DepartmentID" = "DepartmentId" 
        WHERE "DepartmentID" IS NULL AND "DepartmentId" IS NOT NULL;
        
        -- Drop foreign key constraint if it exists
        IF EXISTS (
            SELECT 1 FROM information_schema.table_constraints tc
            JOIN information_schema.constraint_column_usage ccu 
            ON tc.constraint_name = ccu.constraint_name
            WHERE tc.constraint_type = 'FOREIGN KEY' 
            AND tc.table_schema = 'app' 
            AND tc.table_name = 'Users'
            AND ccu.column_name = 'DepartmentId'
        ) THEN
            EXECUTE (
                SELECT 'ALTER TABLE app."Users" DROP CONSTRAINT "' || tc.constraint_name || '"'
                FROM information_schema.table_constraints tc
                JOIN information_schema.constraint_column_usage ccu 
                ON tc.constraint_name = ccu.constraint_name
                WHERE tc.constraint_type = 'FOREIGN KEY' 
                AND tc.table_schema = 'app' 
                AND tc.table_name = 'Users'
                AND ccu.column_name = 'DepartmentId'
                LIMIT 1
            );
        END IF;
        
        -- Drop index if it exists
        DROP INDEX IF EXISTS app."IX_Users_DepartmentId";
        
        -- Drop column
        ALTER TABLE app."Users" DROP COLUMN "DepartmentId";
        
        RAISE NOTICE 'DepartmentId shadow property removed';
    ELSE
        RAISE NOTICE 'DepartmentId shadow property not found';
    END IF;
    
    -- Check if both UserRoleID and UserRoleId exist
    IF EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_schema = 'app' 
        AND table_name = 'Users' 
        AND column_name = 'UserRoleId'
    ) THEN
        -- Copy data from shadow property to main property if needed
        UPDATE app."Users" 
        SET "UserRoleID" = "UserRoleId" 
        WHERE "UserRoleID" IS NULL AND "UserRoleId" IS NOT NULL;
        
        -- Drop foreign key constraint if it exists
        IF EXISTS (
            SELECT 1 FROM information_schema.table_constraints tc
            JOIN information_schema.constraint_column_usage ccu 
            ON tc.constraint_name = ccu.constraint_name
            WHERE tc.constraint_type = 'FOREIGN KEY' 
            AND tc.table_schema = 'app' 
            AND tc.table_name = 'Users'
            AND ccu.column_name = 'UserRoleId'
        ) THEN
            EXECUTE (
                SELECT 'ALTER TABLE app."Users" DROP CONSTRAINT "' || tc.constraint_name || '"'
                FROM information_schema.table_constraints tc
                JOIN information_schema.constraint_column_usage ccu 
                ON tc.constraint_name = ccu.constraint_name
                WHERE tc.constraint_type = 'FOREIGN KEY' 
                AND tc.table_schema = 'app' 
                AND tc.table_name = 'Users'
                AND ccu.column_name = 'UserRoleId'
                LIMIT 1
            );
        END IF;
        
        -- Drop index if it exists
        DROP INDEX IF EXISTS app."IX_Users_UserRoleId";
        
        -- Drop column
        ALTER TABLE app."Users" DROP COLUMN "UserRoleId";
        
        RAISE NOTICE 'UserRoleId shadow property removed';
    ELSE
        RAISE NOTICE 'UserRoleId shadow property not found';
    END IF;
END $$;

-- 2. Fix -infinity dates
DO $$
DECLARE
    table_name text;
    updated_count integer;
BEGIN
    FOR table_name IN 
        SELECT table_name FROM information_schema.tables 
        WHERE table_schema = 'app' 
        AND table_type = 'BASE TABLE'
    LOOP
        -- Check and fix Created column
        IF EXISTS (
            SELECT 1 FROM information_schema.columns 
            WHERE table_schema = 'app' 
            AND table_name = table_name 
            AND column_name = 'Created'
        ) THEN
            EXECUTE format('
                UPDATE app.%I SET "Created" = CURRENT_TIMESTAMP
                WHERE "Created" = ''-infinity''::timestamp', 
                table_name
            );
            
            GET DIAGNOSTICS updated_count = ROW_COUNT;
            IF updated_count > 0 THEN
                RAISE NOTICE 'Fixed % -infinity Created dates in %', updated_count, table_name;
            END IF;
        END IF;
        
        -- Check and fix LastModified column
        IF EXISTS (
            SELECT 1 FROM information_schema.columns 
            WHERE table_schema = 'app' 
            AND table_name = table_name 
            AND column_name = 'LastModified'
        ) THEN
            EXECUTE format('
                UPDATE app.%I SET "LastModified" = CURRENT_TIMESTAMP
                WHERE "LastModified" = ''-infinity''::timestamp', 
                table_name
            );
            
            GET DIAGNOSTICS updated_count = ROW_COUNT;
            IF updated_count > 0 THEN
                RAISE NOTICE 'Fixed % -infinity LastModified dates in %', updated_count, table_name;
            END IF;
        END IF;
    END LOOP;
END $$;

-- 3. Add missing roles
INSERT INTO app."UserRoles" ("Id", "UserRoleName", "Created", "LastModified")
VALUES 
    ('11111111-1111-1111-2222-111111111111', 'Software Developer', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    ('66666666-6666-6666-2222-666666666666', 'DevOps Engineer', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    ('88888888-8888-8888-2222-888888888888', 'System Analyst', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    ('55555555-5555-5555-2222-555555555555', 'QA Engineer', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    ('22222222-2222-2222-2222-222222222222', 'UI/UX Designer', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    ('33333333-3333-3333-2222-333333333333', 'Project Manager', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    ('77777777-7777-7777-2222-777777777777', 'Product Manager', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    ('44444444-4444-4444-2222-444444444444', 'Business Analyst', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    ('99999999-9999-9999-2222-999999999999', 'HR Specialist', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    ('aaaaaaaa-aaaa-aaaa-2222-aaaaaaaaaaaa', 'Marketing Specialist', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    ('bbbbbbbb-bbbb-bbbb-2222-bbbbbbbbbbbb', 'Sales Representative', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
ON CONFLICT ("Id") DO UPDATE 
SET "UserRoleName" = EXCLUDED."UserRoleName";

-- 4. Add missing departments
INSERT INTO app."Departments" ("Id", "DepartmentName", "Created", "LastModified")
VALUES 
    ('3d490a70-94ce-4d15-9494-5248280c2ce3', 'IT', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    ('7c9e6679-7425-40de-944b-e07fc1f90ae7', 'HR', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    ('98a52f9d-16be-4a4f-a6c9-6e9df8e1e6eb', 'Marketing', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    ('f8c3de3d-1fea-4d7c-a8b0-29f63c4c3454', 'Business', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    ('6a534922-c788-4386-a38c-aabc856bdca7', 'Design', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    ('f4ed6c3a-c6d3-47b9-b7e5-a67893a8b3a2', 'Sales', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    ('74b2c633-f052-4e50-b00c-9a4f6a2599d6', 'Management', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
ON CONFLICT ("Id") DO UPDATE 
SET "DepartmentName" = EXCLUDED."DepartmentName";

-- 5. Add a test user for debugging (only if not exists)
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM app."Users" 
        WHERE "EmailAddress" = 'test@example.com'
    ) THEN
        INSERT INTO app."Users" (
            "Id", "UserId", "FirstName", "LastName", 
            "EmailAddress", "Password", "Address", 
            "PhoneNumber", "Gender", "Created", 
            "LastModified", "DepartmentID", "UserRoleID", "UserTypeID"
        ) VALUES (
            '00000000-0000-0000-0000-000000000001', '000001', 'Test', 'User',
            'test@example.com', 'Pass@123', '123 Test St',
            '+1234567890', 0, CURRENT_TIMESTAMP,
            CURRENT_TIMESTAMP, 
            '3d490a70-94ce-4d15-9494-5248280c2ce3', -- IT Department
            '11111111-1111-1111-2222-111111111111', -- Software Developer 
            'c7b013f0-5201-4317-abd8-c211f91b7330'  -- Regular User type
        );
        
        RAISE NOTICE 'Test user created';
    ELSE
        RAISE NOTICE 'Test user already exists';
    END IF;
END $$; 