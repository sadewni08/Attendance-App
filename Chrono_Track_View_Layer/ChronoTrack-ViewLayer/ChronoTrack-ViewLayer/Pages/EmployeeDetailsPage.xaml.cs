using System;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using ChronoTrack_ViewLayer.Models;
using ChronoTrack_ViewLayer.Services;
using ChronoTrack_ViewLayer.Utilities;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.IO;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.Storage;

namespace ChronoTrack_ViewLayer.Pages;

public partial class EmployeeDetailsPage : ContentPage
{
    private EmployeeService _employeeService;
    private ObservableCollection<EmployeeDto> _employees;
    private ObservableCollection<EmployeeDto> _allEmployees;
    private string _searchTerm = "";
    private int _currentPage = 1;
    private int _pageSize = 10;
    private int _totalItems = 0;
    private int _lastEmployeeId = 0;
    private bool _isEditMode = false;
    private Guid? _currentEmployeeId = null;

    public EmployeeDetailsPage()
    {
        InitializeComponent();
        
        // Initialize services and collections
        _employeeService = new EmployeeService();
        _employees = new ObservableCollection<EmployeeDto>();
        EmployeesCollection.ItemsSource = _employees;
        
        // Initialize pagination binding context
        UpdatePaginationBindingContext();
    }

    private void UpdatePaginationBindingContext()
    {
        this.BindingContext = new
        {
            StartItem = (_currentPage - 1) * _pageSize + 1,
            EndItem = Math.Min(_currentPage * _pageSize, _totalItems),
            TotalItems = _totalItems,
            CurrentPage = _currentPage
        };
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        
        // Update sidebar active state
        if (SidebarComponent != null)
        {
            SidebarComponent.IsDashboardActive = false;
            SidebarComponent.IsHistoryActive = false;
            SidebarComponent.IsEmployeeDetailsActive = true;
        }
        
        // Load employee data
        LoadInitialDataAsync();
        
        // Initialize role picker with default department (IT) roles
        string defaultDepartment = "IT";
        DepartmentPicker.SelectedItem = defaultDepartment;
        OnDepartmentSelectedChanged(DepartmentPicker, EventArgs.Empty);
        
        // Add example employee Ram Charan with contact details
        AddExampleEmployeeAsync();
    }

    private async void LoadInitialDataAsync()
    {
        await LoadEmployeesAsync();
    }

    // Handler for department selection change
    private void OnDepartmentSelectedChanged(object sender, EventArgs e)
    {
        try
        {
            if (DepartmentPicker.SelectedItem != null)
            {
                string selectedDepartment = DepartmentPicker.SelectedItem.ToString();
                 
                // Get roles for the selected department
                var departmentRoles = RoleService.GetRolesForDepartment(selectedDepartment);
                
                // Clear existing items
                RolePicker.Items.Clear();
                
                // Add roles for this department
                foreach (var role in departmentRoles)
                {
                    RolePicker.Items.Add(role);
                }
                
                // Select the first role by default if there are any roles
                if (RolePicker.Items.Count > 0)
                {
                    RolePicker.SelectedIndex = 0;
                }
                
                Console.WriteLine($"Updated roles for department: {selectedDepartment}, {departmentRoles.Count} roles available");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating roles for department: {ex.Message}");
        }
    }

    private async Task LoadEmployeesAsync()
    {
        try
        {
            Console.WriteLine($"Loading employees for page {_currentPage}");
            var result = await _employeeService.GetAllEmployeesAsync(_currentPage, _pageSize);
            
            if (result.Success && result.Data != null)
            {
                _employees.Clear();
                _allEmployees = new ObservableCollection<EmployeeDto>();
                Console.WriteLine($"Received {result.Data.Items.Count} employees");
                
                foreach (var employee in result.Data.Items)
                {
                    Console.WriteLine($"Processing employee from API: UserId={employee.UserId}, FullName={employee.FullName}, Gender={employee.Gender ?? "null"}, Email={employee.EmailAddress ?? "null"}");
                    Console.WriteLine($"Additional employee data: Address={employee.Address ?? "null"}, PhoneNumber={employee.PhoneNumber ?? "null"}");
                    
                    // Match the fields to what's shown in the chart
                    var employeeDto = new EmployeeDto
                    {
                        // UserId should be mapped directly from the API response
                        UserId = employee.UserId,
                        
                        // Split the full name into first and last name if needed
                        // This is for internal use/editing but not displayed directly
                        FirstName = employee.FullName.Split(' ').FirstOrDefault() ?? string.Empty,
                        LastName = employee.FullName.Split(' ').Skip(1).FirstOrDefault() ?? string.Empty,
                        
                        // Set FullName property which will be used by the EmployeeName getter
                        FullName = employee.FullName,
                        
                        // Use the department and role directly from the API
                        Department = employee.Department,
                        Role = employee.Role,
                        
                        // Make sure contact details are correctly mapped
                        EmailAddress = employee.EmailAddress ?? string.Empty,
                        PhoneNumber = employee.PhoneNumber ?? string.Empty,
                        Address = employee.Address ?? string.Empty,
                        
                        // Ensure gender is properly mapped
                        Gender = employee.Gender ?? string.Empty,
                        
                        // Set the ID if available, otherwise use a placeholder
                        Id = employee.Id != Guid.Empty ? employee.Id : Guid.NewGuid()
                    };
                    
                    // Enhanced logging for critical fields
                    if (string.IsNullOrEmpty(employee.EmailAddress))
                    {
                        Console.WriteLine($"Note: Employee {employeeDto.UserId} ({employeeDto.EmployeeName}) has no email address");
                    }
                    
                    if (string.IsNullOrEmpty(employee.Gender))
                    {
                        Console.WriteLine($"Note: Employee {employeeDto.UserId} ({employeeDto.EmployeeName}) has no gender specified");
                    }
                    
                    Console.WriteLine($"Added employee: ID={employeeDto.UserId}, Name={employeeDto.EmployeeName}, Gender={employeeDto.Gender}, Dept={employeeDto.Department}, Role={employeeDto.Role}, Email={employeeDto.EmailAddress}");
                    _allEmployees.Add(employeeDto);
                    
                    // Update the _lastEmployeeId if this UserId is numeric and higher than current value
                    if (int.TryParse(employee.UserId, out int userId) && userId > _lastEmployeeId)
                    {
                        _lastEmployeeId = userId;
                        Console.WriteLine($"Updated _lastEmployeeId to {_lastEmployeeId}");
                    }
                }
                
                // Apply search filter if any exists
                ApplySearchFilter();
                
                _totalItems = _allEmployees.Count;
                
                // Update pagination info
                UpdatePaginationBindingContext();
                
                Console.WriteLine($"Updated binding context: Items {(_currentPage - 1) * _pageSize + 1} to {Math.Min(_currentPage * _pageSize, _totalItems)} of {_totalItems}");
            }
            else
            {
                Console.WriteLine($"Failed to load employees: {result.Message ?? "Unknown error"}");
                await DisplayAlert("Error", result.Message ?? "Failed to load employees", "OK");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception in LoadEmployeesAsync: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
        }
    }

    // New method to handle search text changes
    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        _searchTerm = e.NewTextValue?.Trim() ?? "";
        ApplySearchFilter();
    }

    // New method to apply search filter to employees
    private void ApplySearchFilter()
    {
        if (_allEmployees == null)
            return;

        _employees.Clear();

        if (string.IsNullOrWhiteSpace(_searchTerm))
        {
            // If no search term, display all employees
            foreach (var employee in _allEmployees)
            {
                _employees.Add(employee);
            }
            return;
        }

        // Case-insensitive search
        string searchTermLower = _searchTerm.ToLowerInvariant();
        
        // Filter employees based on search criteria (name, ID, department, role)
        var filteredEmployees = _allEmployees.Where(emp => 
            emp.EmployeeName.ToLowerInvariant().Contains(searchTermLower) ||
            emp.UserId.ToLowerInvariant().Contains(searchTermLower) ||
            (emp.Department != null && emp.Department.ToLowerInvariant().Contains(searchTermLower)) ||
            (emp.Role != null && emp.Role.ToLowerInvariant().Contains(searchTermLower))
        );

        foreach (var employee in filteredEmployees)
        {
            _employees.Add(employee);
        }

        Console.WriteLine($"Search filter applied: Found {_employees.Count} matches for '{_searchTerm}'");
    }

    private async void OnAddEmployeeClicked(object sender, EventArgs e)
    {
        _isEditMode = false;
        _currentEmployeeId = null;
        
        // Get the latest employee count for ID generation
        try
        {
            // Get the total count from the paged response
            var result = await _employeeService.GetAllEmployeesAsync(1, 1);
            int count = result.Success && result.Data != null ? result.Data.TotalItems : 0;
            
            _lastEmployeeId = Math.Max(_lastEmployeeId, count);
            
            // Increment by 1 to get the next ID
            int nextEmployeeId = _lastEmployeeId + 1;
            
            // Format as 6-digit number
            EmployeeIdEntry.Text = nextEmployeeId.ToString("D6");
            Console.WriteLine($"Generated next employee ID: {EmployeeIdEntry.Text} (from _lastEmployeeId: {_lastEmployeeId})");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting employee count: {ex.Message}");
            // Use a default value if there's an error
            EmployeeIdEntry.Text = (_lastEmployeeId + 1).ToString("D6");
        }
        
        // Reset form fields
        FirstNameEntry.Text = string.Empty;
        LastNameEntry.Text = string.Empty;
        GenderPicker.SelectedIndex = -1;
        DepartmentPicker.SelectedIndex = -1;
        RolePicker.SelectedIndex = -1;
        AddressEntry.Text = string.Empty;
        PhoneEntry.Text = string.Empty;
        EmailEntry.Text = string.Empty; // Clear email field
        
        // Update form title
        FormTitleLabel.Text = "Add New Employee";
        
        // Show the form
        AddEmployeeFormOverlay.IsVisible = true;
    }

    private void OnCloseFormClicked(object sender, EventArgs e)
    {
        AddEmployeeFormOverlay.IsVisible = false;
    }

    private async void OnSaveEmployeeClicked(object sender, EventArgs e)
    {
        try
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(EmployeeIdEntry.Text) ||
                string.IsNullOrWhiteSpace(FirstNameEntry.Text) ||
                GenderPicker.SelectedItem == null ||
                DepartmentPicker.SelectedItem == null ||
                RolePicker.SelectedItem == null)
            {
                await DisplayAlert("Validation Error", "Please fill in all required fields: ID, Name, Gender, Department, and Role", "OK");
                return;
            }
            
            // Validate email format if provided
            if (!string.IsNullOrWhiteSpace(EmailEntry.Text) && !IsValidEmail(EmailEntry.Text))
            {
                await DisplayAlert("Validation Error", "Please enter a valid email address", "OK");
                return;
            }

            // Get department ID
            var departmentName = DepartmentPicker.SelectedItem?.ToString();
            var departmentId = await GetDepartmentIdByNameAsync(departmentName);
            
            // Get role ID
            var roleName = RolePicker.SelectedItem?.ToString();
            var roleId = await GetRoleIdByNameAsync(roleName);
            
            // Get gender value
            var gender = GenderPicker.SelectedItem?.ToString() ?? "Male";
            
            Console.WriteLine($"Saving employee data:");
            Console.WriteLine($"  - ID: {EmployeeIdEntry.Text}");
            Console.WriteLine($"  - Name: {FirstNameEntry.Text} {LastNameEntry.Text}");
            Console.WriteLine($"  - Gender: {gender}");
            Console.WriteLine($"  - Department: {departmentName} (ID: {departmentId})");
            Console.WriteLine($"  - Role: {roleName} (ID: {roleId})");
            Console.WriteLine($"  - Email: {EmailEntry.Text ?? "Not provided"}");
            Console.WriteLine($"  - Phone: {PhoneEntry.Text ?? "Not provided"}");
            Console.WriteLine($"  - Address: {AddressEntry.Text ?? "Not provided"}");
            
            if (_isEditMode)
            {
                // UPDATE: Edit existing employee
                var updateDto = new UpdateEmployeeDto
                {
                    UserId = EmployeeIdEntry.Text,
                    FirstName = FirstNameEntry.Text,
                    LastName = LastNameEntry.Text ?? string.Empty,
                    Gender = gender,
                    Address = AddressEntry.Text ?? string.Empty,
                    EmailAddress = EmailEntry.Text ?? string.Empty,
                    PhoneNumber = PhoneEntry.Text ?? string.Empty,
                    DepartmentID = departmentId,
                    UserRoleID = roleId
                };

                // Use userId to update the employee instead of Guid
                Console.WriteLine($"Updating employee with UserId: {EmployeeIdEntry.Text}");
                var result = await _employeeService.UpdateEmployeeAsync(EmployeeIdEntry.Text, updateDto);
                
                if (result.Success)
                {
                    Console.WriteLine("Employee updated successfully");
                    await DisplayAlert("Success", "Employee updated successfully", "OK");
                }
                else
                {
                    Console.WriteLine($"Failed to update employee: {result.Message}");
                    await DisplayAlert("Error", $"Failed to update employee: {result.Message}", "OK");
                    return;
                }
            }
            else
            {
                // CREATE: Add new employee
                var createDto = new CreateEmployeeDto
                {
                    UserId = EmployeeIdEntry.Text,
                    FirstName = FirstNameEntry.Text,
                    LastName = LastNameEntry.Text ?? string.Empty,
                    Gender = gender,
                    Address = AddressEntry.Text ?? string.Empty,
                    EmailAddress = EmailEntry.Text ?? string.Empty,
                    PhoneNumber = PhoneEntry.Text ?? string.Empty,
                    Password = "Pass@123", // Default password
                    DepartmentID = departmentId,
                    UserRoleID = roleId,
                    UserTypeID = Guid.Parse("c7b013f0-5201-4317-abd8-c211f91b7330") // Regular user type
                };

                Console.WriteLine("Creating new employee");
                var result = await _employeeService.CreateEmployeeAsync(createDto);
                
                if (result.Success)
                {
                    Console.WriteLine("Employee created successfully");
                    await DisplayAlert("Success", "Employee created successfully", "OK");
                }
                else
                {
                    Console.WriteLine($"Failed to create employee: {result.Message}");
                    await DisplayAlert("Error", $"Failed to create employee: {result.Message}", "OK");
                    return;
                }
            }

            // Hide the form and reload data
            AddEmployeeFormOverlay.IsVisible = false;
            await LoadEmployeesAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving employee: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
        }
    }

    // Helper method for testing API directly
    private async Task<string> TestApiDirectly(Guid employeeId, UpdateEmployeeDto updateDto)
    {
        return await ApiTester.TestUpdateEmployeeEndpoint(employeeId, updateDto);
    }

    private void OnEditClicked(object sender, EventArgs e)
    {
        try
        {
            var button = (Button)sender;
            var employee = (EmployeeDto)button.CommandParameter;
            
            Console.WriteLine($"Edit clicked for employee: {employee.UserId}, {employee.EmployeeName}");
            
            // Set edit mode flag 
            _isEditMode = true;
            
            // Update form title
            FormTitleLabel.Text = "Edit Employee";
            
            // Fill form with employee data
            EmployeeIdEntry.Text = employee.UserId;
            EmployeeIdEntry.IsReadOnly = true; // Cannot change employee ID during edit
            
            FirstNameEntry.Text = employee.FirstName;
            LastNameEntry.Text = employee.LastName;
            
            // Set Gender
            int genderIndex = GetGenderIndex(employee.Gender);
            GenderPicker.SelectedIndex = genderIndex;
            Console.WriteLine($"Setting gender to {employee.Gender} (index {genderIndex})");
            
            // Set Department
            SetPickerValue(DepartmentPicker, employee.Department);
            
            // Set Role after department is set
            SetPickerValue(RolePicker, employee.Role);
            
            // Set contact details
            EmailEntry.Text = employee.EmailAddress;
            PhoneEntry.Text = employee.PhoneNumber;
            AddressEntry.Text = employee.Address;
            
            // Show the form
            AddEmployeeFormOverlay.IsVisible = true;
            
            Console.WriteLine($"Edit form populated for employee: {employee.UserId}, {employee.EmployeeName}");
            Console.WriteLine($"Contact details: Email={employee.EmailAddress}, Phone={employee.PhoneNumber}, Address={employee.Address}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in OnEditClicked: {ex.Message}");
            DisplayAlert("Error", $"An error occurred while preparing the edit form: {ex.Message}", "OK");
        }
    }

    // Helper method to convert gender string to picker index
    private int GetGenderIndex(string gender)
    {
        return gender.ToLower() switch
        {
            "male" => 0,
            "female" => 1,
            "other" => 2,
            _ => 0 // Default to Male if unknown
        };
    }

    private void SetPickerValue(Picker picker, string value)
    {
        if (picker == null || string.IsNullOrEmpty(value))
            return;
            
        // Find the index of the value in the picker items
        for (int i = 0; i < picker.Items.Count; i++)
        {
            if (picker.Items[i].Equals(value, StringComparison.OrdinalIgnoreCase))
            {
                picker.SelectedIndex = i;
                return;
            }
        }
        
        // If the value is not found in the picker items
        // and this is the RolePicker, we may need to add it first
        if (picker == RolePicker)
        {
            // Add the role to the picker if it's not already there
            picker.Items.Add(value);
            picker.SelectedIndex = picker.Items.Count - 1;
        }
        else
        {
            // For other pickers, default to first item if available
            if (picker.Items.Count > 0)
            {
                picker.SelectedIndex = 0;
            }
        }
    }

    private async void OnDeleteClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is EmployeeDto employee)
        {
            try
            {
                Console.WriteLine($"Delete requested for employee: {employee.EmployeeName}");
                Console.WriteLine($"Using userId: {employee.UserId}");

                // Ask for confirmation before deleting
                bool answer = await DisplayAlert(
                    "Delete Employee", 
                    $"Are you sure you want to delete employee {employee.EmployeeName}?\n\nThis action cannot be undone.", 
                    "Yes, Delete", 
                    "Cancel"
                );

                if (answer)
                {
                    Console.WriteLine("User confirmed deletion, proceeding...");
                    
                    // Show a loading indicator or disable UI elements here if needed
                    
                    // Call the service to delete the employee using userId instead of Guid
                    var result = await _employeeService.DeleteEmployeeAsync(employee.UserId);
                    
                    if (result.Success)
                    {
                        Console.WriteLine("Employee deleted successfully");
                        await DisplayAlert("Success", "Employee deleted successfully", "OK");
                        
                        // Remove from local collection first for immediate UI update
                        var employeeToRemove = _employees.FirstOrDefault(e => e.UserId == employee.UserId);
                        if (employeeToRemove != null)
                        {
                            _employees.Remove(employeeToRemove);
                        }
                        
                        // Then reload data from server to ensure we're in sync
                        await LoadEmployeesAsync();
                    }
                    else
                    {
                        Console.WriteLine($"Failed to delete employee: {result.Message}");
                        await DisplayAlert("Error", result.Message ?? "Failed to delete employee", "OK");
                    }
                }
                else
                {
                    Console.WriteLine("User cancelled deletion");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in OnDeleteClicked: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                await DisplayAlert("Error", $"An error occurred while deleting the employee: {ex.Message}", "OK");
            }
        }
    }

    private async void OnExportEmployeesClicked(object sender, EventArgs e)
    {
        try
        {
            // Create a loading indicator
            var loadingAlert = await DisplayAlert("Export Employees", "Would you like to export all employee details to a CSV file?", "Yes", "No");
            if (!loadingAlert)
                return;
            
            // Create CSV content with headers
            var csvContent = new StringBuilder();
            csvContent.AppendLine("Employee ID,Full Name,Department,Role,Gender,Email Address,Phone Number,Address");
            
            // Fetch all employees if not already loaded
            if (_allEmployees == null || _allEmployees.Count == 0)
            {
                await LoadEmployeesAsync();
            }
            
            if (_allEmployees == null || _allEmployees.Count == 0)
            {
                await DisplayAlert("Export Error", "No employees found to export.", "OK");
                return;
            }
            
            // Add each employee as a row in the CSV
            foreach (var employee in _allEmployees)
            {
                // Escape any commas in fields by enclosing in double quotes
                string fullName = employee.EmployeeName;
                if (fullName.Contains(",")) fullName = $"\"{fullName}\"";
                
                string department = employee.Department ?? "";
                if (department.Contains(",")) department = $"\"{department}\"";
                
                string role = employee.Role ?? "";
                if (role.Contains(",")) role = $"\"{role}\"";
                
                string address = employee.Address ?? "";
                if (address.Contains(",")) address = $"\"{address}\"";
                
                string email = employee.EmailAddress ?? "";
                if (email.Contains(",")) email = $"\"{email}\"";
                
                string phone = employee.PhoneNumber ?? "";
                if (phone.Contains(",")) phone = $"\"{phone}\"";
                
                string gender = employee.Gender ?? "";
                if (gender.Contains(",")) gender = $"\"{gender}\"";
                
                // Append employee data as CSV row
                csvContent.AppendLine($"{employee.UserId},{fullName},{department},{role},{gender},{email},{phone},{address}");
            }
            
            // Get the file path for saving the CSV
            string fileName = $"EmployeeExport_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            string filePath = Path.Combine(FileSystem.CacheDirectory, fileName);
            
            // Write CSV content to file
            File.WriteAllText(filePath, csvContent.ToString());
            
            // Share the file
            await Share.RequestAsync(new ShareFileRequest
            {
                Title = "Employee Data Export",
                File = new ShareFile(filePath)
            });
            
            Console.WriteLine($"CSV file exported successfully: {filePath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error exporting employees: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            await DisplayAlert("Export Error", $"Failed to export employees: {ex.Message}", "OK");
        }
    }

    private async void OnPreviousClicked(object sender, EventArgs e)
    {
        if (_currentPage > 1)
        {
            _currentPage--;
            // Instead of reloading from server, just update the displayed items
            UpdateDisplayedEmployees();
        }
    }

    private async void OnNextClicked(object sender, EventArgs e)
    {
        var totalPages = (int)Math.Ceiling((double)_employees.Count / _pageSize);
        if (_currentPage < totalPages)
        {
            _currentPage++;
            // Instead of reloading from server, just update the displayed items
            UpdateDisplayedEmployees();
        }
    }

    private async void OnPageNumberClicked(object sender, EventArgs e)
    {
        if (sender is Button button && int.TryParse(button.Text, out int page))
        {
            _currentPage = page;
            // Instead of reloading from server, just update the displayed items
            UpdateDisplayedEmployees();
        }
    }

    private async void OnRefreshData(object sender, EventArgs e)
    {
        try
        {
            Console.WriteLine("Refreshing employee data...");
            
            // Reset to first page
            _currentPage = 1;
            
            // Clear search term
            SearchEntry.Text = "";
            _searchTerm = "";
            
            // Reload data
            await LoadEmployeesAsync();
            
            // Complete the refresh
            EmployeeRefreshView.IsRefreshing = false;
            
            Console.WriteLine("Employee data refreshed successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error refreshing employee data: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            EmployeeRefreshView.IsRefreshing = false;
        }
    }

    // Update method to handle pagination and chart data
    private async void UpdateDisplayedEmployees()
    {
        try
        {
            Console.WriteLine($"Updating displayed employees for page {_currentPage}");
            
            // Reload data from server for the current page
            var result = await _employeeService.GetAllEmployeesAsync(_currentPage, _pageSize);
            
            if (result.Success && result.Data != null)
            {
                _employees.Clear();
                
                foreach (var employee in result.Data.Items)
                {
                    _employees.Add(employee);
                }
                
                // Update total items count
                _totalItems = result.Data.TotalItems;
                
                // Update pagination binding context with all required values
                UpdatePaginationBindingContext();
                
                Console.WriteLine($"Updated display: Showing items {(_currentPage - 1) * _pageSize + 1} to {Math.Min(_currentPage * _pageSize, _totalItems)} of {_totalItems}");
                Console.WriteLine($"Current page: {_currentPage}");
                
                // Update chart data based on the current page data
                UpdateChartData();
            }
            else
            {
                Console.WriteLine($"Failed to update displayed employees: {result.Message ?? "Unknown error"}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating displayed employees: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }
    
    private void UpdateChartData()
    {
        try
        {
            // Calculate department distribution
            var departmentGroups = _employees
                .GroupBy(e => e.Department)
                .Select(g => new { Department = g.Key, Count = g.Count() })
                .ToList();
                
            // Calculate gender distribution
            var genderGroups = _employees
                .GroupBy(e => e.Gender)
                .Select(g => new { Gender = g.Key, Count = g.Count() })
                .ToList();
                
            // Calculate role distribution
            var roleGroups = _employees
                .GroupBy(e => e.Role)
                .Select(g => new { Role = g.Key, Count = g.Count() })
                .ToList();
                
            // Update your chart controls here with the new data
            // Example:
            // DepartmentChart.ItemsSource = departmentGroups;
            // GenderChart.ItemsSource = genderGroups;
            // RoleChart.ItemsSource = roleGroups;
            
            Console.WriteLine("Chart data updated successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating chart data: {ex.Message}");
        }
    }

    // Add this method after the class methods
    private bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    private async Task AddExampleEmployeeAsync()
    {
        try
        {
            // Check if we already have an employee with ID "000001"
            var existingEmployee = _employees.FirstOrDefault(e => e.UserId == "000001");
            if (existingEmployee != null)
            {
                Console.WriteLine("Example employee Ram Charan already exists");
                return;
            }
            
            Console.WriteLine("Adding example employee Ram Charan with contact details");
            
            // Create the example employee with all required details
            var ramCharan = new CreateEmployeeDto
            {
                UserId = "000001",
                FirstName = "Ram",
                LastName = "Charan",
                Gender = "Male",
                Address = "123 Tech Street, Colombo 01",
                EmailAddress = "ram.charan@ateamsoftware.com",
                PhoneNumber = "+94 77 1234567",
                Password = "Pass@123",
                // Get the correct GUIDs for the Department and Role
                DepartmentID = await GetDepartmentIdByNameAsync("Information Technology"),
                UserRoleID = await GetRoleIdByNameAsync("Software Developer")
            };
            
            // Add the employee via the API
            var result = await _employeeService.CreateEmployeeAsync(ramCharan);
            
            if (result.Success)
            {
                Console.WriteLine("Successfully added example employee Ram Charan");
                // Refresh the employee list to show the new entry
                await LoadEmployeesAsync();
            }
            else
            {
                Console.WriteLine($"Failed to add example employee: {result.Message}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error adding example employee: {ex.Message}");
        }
    }
    
    private async Task<Guid> GetDepartmentIdByNameAsync(string departmentName)
    {
        // This is a placeholder implementation - in a real app you'd fetch this from the API
        // For this example, we'll use a hardcoded value for Information Technology
        return Guid.Parse("3d490a70-94ce-4d15-9494-5248280c2ce3");
    }
    
    private async Task<Guid> GetRoleIdByNameAsync(string roleName)
    {
        // This is a placeholder implementation - in a real app you'd fetch this from the API
        // For this example, we'll use a hardcoded value for Software Developer
        return Guid.Parse("a9f1d24f-5b5a-4a87-a1ed-e6541b4a6fec");
    }
} 