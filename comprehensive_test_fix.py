#!/usr/bin/env python3

import os
import re
import glob

def fix_readonly_properties():
    """Fix read-only property assignments by using proper constructors"""
    
    # Define entity constructors and their parameters
    entity_constructors = {
        'User': ['string name', 'string email', 'UserRole role', 'bool isActive = true'],
        'AuditLog': ['string action', 'string details', 'Guid? userId = null', 'string ipAddress = null', 'string userAgent = null'],
        'UserSession': ['Guid userId', 'string token', 'DateTime expiresAt'],
        'Fee': ['Guid studentId', 'decimal amount', 'FeeType feeType', 'string description'],
        'Performance': ['Guid studentId', 'Guid facultyId', 'string subject', 'decimal score'],
        'Student': ['string name', 'string email', 'DateTime dateOfBirth'],
        'Faculty': ['string name', 'string email', 'string department']
    }
    
    test_files = glob.glob('tests/Api/EduShield.Api.Tests/*.cs')
    
    for file_path in test_files:
        print(f"Processing {file_path}...")
        
        with open(file_path, 'r') as f:
            content = f.read()
        
        original_content = content
        
        # Fix User entity creation
        content = re.sub(
            r'new User\s*\{\s*Id\s*=\s*[^,}]+,?\s*Name\s*=\s*([^,}]+),?\s*Email\s*=\s*([^,}]+),?\s*Role\s*=\s*([^,}]+),?\s*IsActive\s*=\s*([^,}]+),?\s*\}',
            r'CreateUser(\1, \2, \3, \4)',
            content,
            flags=re.MULTILINE | re.DOTALL
        )
        
        # Fix AuditLog entity creation
        content = re.sub(
            r'new AuditLog\s*\{\s*Id\s*=\s*[^,}]+,?\s*Action\s*=\s*([^,}]+),?\s*Details\s*=\s*([^,}]+),?\s*UserId\s*=\s*([^,}]+),?\s*IpAddress\s*=\s*([^,}]+),?\s*UserAgent\s*=\s*([^,}]+),?\s*Timestamp\s*=\s*[^,}]+,?\s*\}',
            r'CreateAuditLog(\1, \2, \3, \4, \5)',
            content,
            flags=re.MULTILINE | re.DOTALL
        )
        
        # Fix UserSession entity creation
        content = re.sub(
            r'new UserSession\s*\{\s*Id\s*=\s*[^,}]+,?\s*UserId\s*=\s*([^,}]+),?\s*Token\s*=\s*([^,}]+),?\s*ExpiresAt\s*=\s*([^,}]+),?\s*\}',
            r'CreateUserSession(\1, \2, \3)',
            content,
            flags=re.MULTILINE | re.DOTALL
        )
        
        # Fix Fee entity creation
        content = re.sub(
            r'new Fee\s*\{\s*Id\s*=\s*[^,}]+,?\s*StudentId\s*=\s*([^,}]+),?\s*Amount\s*=\s*([^,}]+),?\s*FeeType\s*=\s*([^,}]+),?\s*Description\s*=\s*([^,}]+),?\s*\}',
            r'CreateFee(\1, \2, \3, \4)',
            content,
            flags=re.MULTILINE | re.DOTALL
        )
        
        # Fix Performance entity creation
        content = re.sub(
            r'new Performance\s*\{\s*Id\s*=\s*[^,}]+,?\s*StudentId\s*=\s*([^,}]+),?\s*FacultyId\s*=\s*([^,}]+),?\s*Subject\s*=\s*([^,}]+),?\s*Score\s*=\s*([^,}]+),?\s*\}',
            r'CreatePerformance(\1, \2, \3, \4)',
            content,
            flags=re.MULTILINE | re.DOTALL
        )
        
        # Fix simple property assignments
        content = re.sub(r'(\w+)\.Id\s*=\s*[^;]+;', '', content)
        content = re.sub(r'(\w+)\.Timestamp\s*=\s*[^;]+;', '', content)
        content = re.sub(r'(\w+)\.Name\s*=\s*[^;]+;', '', content)
        content = re.sub(r'(\w+)\.Token\s*=\s*[^;]+;', '', content)
        content = re.sub(r'(\w+)\.Details\s*=\s*[^;]+;', '', content)
        
        if content != original_content:
            with open(file_path, 'w') as f:
                f.write(content)
            print(f"Fixed readonly properties in {file_path}")

def fix_mock_setups():
    """Fix mock setup issues with expression trees"""
    
    test_files = glob.glob('tests/Api/EduShield.Api.Tests/*.cs')
    
    for file_path in test_files:
        with open(file_path, 'r') as f:
            content = f.read()
        
        original_content = content
        
        # Fix expression tree issues with It.IsAny<CancellationToken>()
        content = re.sub(
            r'\.Setup\(([^)]+)\s*,\s*It\.IsAny<CancellationToken>\(\)\)',
            r'.Setup(\1)',
            content
        )
        
        # Fix ReturnsAsync issues
        content = re.sub(
            r'\.Setup\(([^)]+)\)\s*\.ReturnsAsync\(([^)]+)\);',
            r'.Setup(\1).ReturnsAsync(\2);',
            content
        )
        
        if content != original_content:
            with open(file_path, 'w') as f:
                f.write(content)
            print(f"Fixed mock setups in {file_path}")

def fix_method_signatures():
    """Fix method signature mismatches"""
    
    test_files = glob.glob('tests/Api/EduShield.Api.Tests/*.cs')
    
    for file_path in test_files:
        with open(file_path, 'r') as f:
            content = f.read()
        
        original_content = content
        
        # Fix LogoutAsync calls - add sessionId parameter
        content = re.sub(
            r'\.LogoutAsync\(([^,)]+)\)',
            r'.LogoutAsync(Guid.NewGuid(), \1, CancellationToken.None)',
            content
        )
        
        # Fix GetAllAsync calls
        content = re.sub(
            r'\.GetAllAsync\((\d+),\s*(\d+)\)',
            r'.GetAllAsync(CancellationToken.None)',
            content
        )
        
        # Fix GetByUserIdAsync calls
        content = re.sub(
            r'\.GetByUserIdAsync\(([^,)]+),\s*(\d+),\s*(\d+)\)',
            r'.GetByUserIdAsync(\1, CancellationToken.None)',
            content
        )
        
        if content != original_content:
            with open(file_path, 'w') as f:
                f.write(content)
            print(f"Fixed method signatures in {file_path}")

def add_helper_methods():
    """Add helper methods for entity creation"""
    
    test_files = glob.glob('tests/Api/EduShield.Api.Tests/*.cs')
    
    helper_methods = '''
    private static User CreateUser(string name, string email, UserRole role, bool isActive = true)
    {
        return new User(name, email, role, isActive);
    }
    
    private static AuditLog CreateAuditLog(string action, string details, Guid? userId = null, string ipAddress = null, string userAgent = null)
    {
        return new AuditLog(action, details, userId, ipAddress, userAgent);
    }
    
    private static UserSession CreateUserSession(Guid userId, string token, DateTime expiresAt)
    {
        return new UserSession(userId, token, expiresAt);
    }
    
    private static Fee CreateFee(Guid studentId, decimal amount, FeeType feeType, string description)
    {
        return new Fee(studentId, amount, feeType, description);
    }
    
    private static Performance CreatePerformance(Guid studentId, Guid facultyId, string subject, decimal score)
    {
        return new Performance(studentId, facultyId, subject, score);
    }
'''
    
    for file_path in test_files:
        with open(file_path, 'r') as f:
            content = f.read()
        
        # Check if helper methods already exist
        if 'CreateUser(' in content:
            continue
            
        # Find the last closing brace of the test class
        class_match = re.search(r'(public class \w+Tests?\s*\{.*?)(\n\s*\})\s*$', content, re.DOTALL)
        if class_match:
            new_content = content[:class_match.end(1)] + helper_methods + class_match.group(2) + content[class_match.end():]
            
            with open(file_path, 'w') as f:
                f.write(new_content)
            print(f"Added helper methods to {file_path}")

def fix_dto_assignments():
    """Fix DTO property assignments that are read-only"""
    
    test_files = glob.glob('tests/Api/EduShield.Api.Tests/*.cs')
    
    for file_path in test_files:
        with open(file_path, 'r') as f:
            content = f.read()
        
        original_content = content
        
        # Fix CreateUserRequest assignments
        content = re.sub(
            r'var\s+(\w+)\s*=\s*new\s+CreateUserRequest\s*\{\s*Name\s*=\s*([^,}]+),?\s*([^}]*)\};',
            r'var \1 = new CreateUserRequest(\2);',
            content
        )
        
        # Fix UpdateUserRequest assignments
        content = re.sub(
            r'var\s+(\w+)\s*=\s*new\s+UpdateUserRequest\s*\{\s*Name\s*=\s*([^,}]+),?\s*([^}]*)\};',
            r'var \1 = new UpdateUserRequest(\2);',
            content
        )
        
        # Fix ExternalUserInfo assignments
        content = re.sub(
            r'var\s+(\w+)\s*=\s*new\s+ExternalUserInfo\s*\{\s*Name\s*=\s*([^,}]+),?\s*([^}]*)\};',
            r'var \1 = new ExternalUserInfo(\2);',
            content
        )
        
        # Fix AuthResult assignments
        content = re.sub(
            r'var\s+(\w+)\s*=\s*new\s+AuthResult\s*\{\s*IsSuccess\s*=\s*([^,}]+),?\s*([^}]*)\};',
            r'var \1 = new AuthResult(\2);',
            content
        )
        
        if content != original_content:
            with open(file_path, 'w') as f:
                f.write(content)
            print(f"Fixed DTO assignments in {file_path}")

def fix_controller_result_types():
    """Fix controller result type casting issues"""
    
    test_files = glob.glob('tests/Api/EduShield.Api.Tests/*ControllerTests.cs')
    
    for file_path in test_files:
        with open(file_path, 'r') as f:
            content = f.read()
        
        original_content = content
        
        # Fix ActionResult casting
        content = re.sub(
            r'var\s+(\w+)\s*=\s*\((\w+)\)\s*result;',
            r'var \1 = result.Result as \2;',
            content
        )
        
        # Fix direct casting
        content = re.sub(
            r'Assert\.That\(result,\s*Is\.TypeOf<(\w+)>\(\)\);',
            r'Assert.That(result.Result, Is.TypeOf<\1>());',
            content
        )
        
        if content != original_content:
            with open(file_path, 'w') as f:
                f.write(content)
            print(f"Fixed controller result types in {file_path}")

def fix_missing_dependencies():
    """Fix missing mock dependencies"""
    
    test_files = glob.glob('tests/Api/EduShield.Api.Tests/*ServiceTests.cs')
    
    for file_path in test_files:
        with open(file_path, 'r') as f:
            content = f.read()
        
        original_content = content
        
        # Add missing _mockAuditService
        if '_mockAuditService' in content and 'Mock<IAuditService> _mockAuditService' not in content:
            # Find the field declarations section
            setup_match = re.search(r'(\[SetUp\]\s*public void Setup\(\)\s*\{)', content)
            if setup_match:
                # Add field declaration before Setup method
                field_declaration = '    private Mock<IAuditService> _mockAuditService;\n\n'
                content = content[:setup_match.start()] + field_declaration + content[setup_match.start():]
                
                # Add initialization in Setup method
                setup_body_match = re.search(r'(\[SetUp\]\s*public void Setup\(\)\s*\{[^}]*)', content)
                if setup_body_match:
                    initialization = '\n        _mockAuditService = new Mock<IAuditService>();'
                    content = content[:setup_body_match.end()] + initialization + content[setup_body_match.end():]
        
        if content != original_content:
            with open(file_path, 'w') as f:
                f.write(content)
            print(f"Fixed missing dependencies in {file_path}")

def main():
    """Main function to run all fixes"""
    print("Starting comprehensive test fixes...")
    
    # Change to the project directory
    os.chdir('.')
    
    # Run all fixes
    fix_readonly_properties()
    fix_mock_setups()
    fix_method_signatures()
    fix_dto_assignments()
    fix_controller_result_types()
    fix_missing_dependencies()
    add_helper_methods()
    
    print("Comprehensive test fixes completed!")

if __name__ == "__main__":
    main()