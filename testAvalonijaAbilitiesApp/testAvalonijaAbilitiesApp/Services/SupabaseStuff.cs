namespace taskManager.Services;


using Supabase;
using System;
using System.Threading.Tasks;
using Supabase.Gotrue; // Ensure correct import for User
using Supabase.Interfaces;
using testAvalonijaAbilitiesApp.DataModel;
using ToDoList.DataModel;
using System.Collections.Generic;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Supabase.Postgrest;
using System.Linq;

public class SupabaseService
{
    private static readonly string SupabaseUrl = "https://sjthxroqprenodvuczum.supabase.co";
    private static readonly string SupabaseKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6InNqdGh4cm9xcHJlbm9kdnVjenVtIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NTMxNzcyNzksImV4cCI6MjA2ODc1MzI3OX0.njRqfFntSHocgOuKKfQ9z7sCmjuPwBWiK8DIuCYY4ko";
    private static Supabase.Client? _supabaseClient;
    public static User? CurrentUser { get; private set; } = null;

    public static async void InitializeSupabase()
    {
        try
        {
            _supabaseClient = new Supabase.Client(SupabaseUrl, SupabaseKey);
            await _supabaseClient.InitializeAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to connect to Supabase: {ex.Message}");
        }
    }

    public static async Task<Supabase.Client> GetSupabaseClient()
    {
        if (_supabaseClient == null)
        {
            _supabaseClient = new Supabase.Client(SupabaseUrl, SupabaseKey);
            await _supabaseClient.InitializeAsync();
        }
        return _supabaseClient;
    }

    public static async Task<User?> TryLogin(string username, string password)
    {
        try
        {
            var supabase = await GetSupabaseClient();
            var response = await supabase.Auth.SignInWithPassword(username, password);
            CurrentUser = response.User; // Set the current user if sign-up is successful

            return response.User; // Return the entire User object if login is successful
        }
        catch (Exception ex) // Catch all exceptions
        {
            if (ex.Message.Contains("invalid_credentials")) // Check for invalid login error
            {
                return null; // Return null for invalid login
            }
            if (ex.Message.Contains("Email not confirmed")) 
            {
                return null; 
            }

            Console.WriteLine("Login failed: " + ex.Message);
            return null; // Return null for any other failure
        }
    }

    [Table("Task")]
    public class SupabaseTask : BaseModel
    {
        [PrimaryKey("id", false)]
        public int Id { get; set; }

        [Column("TaskDescription")]
        public string TaskDescription { get; set; }

        [Column("IsDone")]
        public bool IsDone { get; set; }

        [Column("DateOfCreation")]
        public DateTime DateOfCreation { get; set; }

        [Column("DueDate")]
        public DateTime? DueDate { get; set; }

        [Column("CurrentIntProgress")]
        public int? CurrentIntProgress { get; set; }

        [Column("MaxIntProgress")]
        public int? MaxIntProgress { get; set; }

        [Column("OwnerUserID")]
        public string OwnerUserID { get; set; }

        [Column("TaskType")]
        public string TaskTypeString { get; set; } 
        [Column("TaskTitle")]
        public string TaskTitle { get; set; } // We'll map this manually
    }

    [Table("TaskRelationship")]
    public class TaskRelationship : BaseModel
    {
        [Column("ParentTaskId")]
        public int ParentTaskID { get; set; }

        [Column("ChildTaskId")]
        public int ChildTaskID { get; set; }
    }

    public static async Task<List<TaskItem>> GetItems()
    {
        var supabase = await GetSupabaseClient();

        var response = await supabase
            .From<SupabaseTask>()
            .Filter("OwnerUserID", Supabase.Postgrest.Constants.Operator.Equals, CurrentUser.Id)
            .Get();

        var tasks = new List<TaskItem>();

        foreach (var record in response.Models)
        {
            TaskType taskType = TaskTypeHelper.GetTaskTypeFromString(record.TaskTypeString);

            TaskItem task = taskType == TaskType.Complex
                ? new ComplexTaskItem()
                : new TaskItem();

            task.TaskID = record.Id;
            task.Title = record.TaskTitle;
            task.TaskDescription = record.TaskDescription;
            task.IsDone = record.IsDone;
            task.TaskType = taskType;
            task.DateOfCreation = record.DateOfCreation;
            task.DueDate = record.DueDate;
            task.CurrentIntProgress = record.CurrentIntProgress;
            task.MaxIntProgress = record.MaxIntProgress;
            task.UserOwner = CurrentUser;

            tasks.Add(task);
        }

        // SetUpSubtasksForComplexTasks(tasks);

        //  foreach (var task in tasks)
        //  {
        //      if (task is ComplexTaskItem complexTask)
        //          complexTask.CalculateProgress();
        //    }

        return tasks;
    }

    public static async Task<int> SignUpNewUser(string userName, string password)
    {
        try
        {
            var supabase = await GetSupabaseClient();

            var response = await supabase.Auth.SignUp(email: userName, password: password);
            if (response.User != null)
            {
                CurrentUser = response.User; // Set the current user if sign-up is successful
                // Success
                return 1;
            }
            else
            {
                // Something went wrong (e.g., verification email sent but not yet confirmed)
                return -1;
            }
        }
        catch (Exception ex)
        {
            if (ex.Message.Contains("User already registered"))
            {
                return -1; // Email already exists
            }

            Console.WriteLine($"Sign up failed: {ex.Message}");
            return -1; // Unknown error
        }
    }

    public static async Task SaveChangesToDatabase(TaskItem itemToSave)
    {
        var supabase = await GetSupabaseClient();

        // Create a SupabaseTask to update
        var taskToUpdate = new SupabaseTask
        {
            Id = itemToSave.TaskID,
            IsDone = itemToSave.IsDone,
            CurrentIntProgress = itemToSave.CurrentIntProgress,
            MaxIntProgress = itemToSave.MaxIntProgress,
            // You can include other fields if needed (usually only updated fields)

            TaskTitle = itemToSave.Title,
            TaskDescription = itemToSave.TaskDescription,
            TaskTypeString = TaskTypeHelper.GetStringFromTaskType(itemToSave.TaskType),
            DateOfCreation = itemToSave.DateOfCreation,
            DueDate = itemToSave.DueDate,
            OwnerUserID = itemToSave.UserOwner.Id
        };

        try
        {
            var response = await supabase
                .From<SupabaseTask>()
                .Where(t => t.Id == itemToSave.TaskID)
                .Update(taskToUpdate);

            if (response.Models.Count > 0)
            {
                Console.WriteLine("Task updated successfully.");
            }
            else
            {
                Console.WriteLine("Task update failed or no rows affected.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating task: {ex.Message}");
        }
    }

    public static async Task DeleteItem(int taskID)
    {
        try
        {
            var supabase = await GetSupabaseClient();

            await supabase
                .From<SupabaseTask>()
                .Where(t => t.Id == taskID)
                .Delete();

            Console.WriteLine($"Task with ID {taskID} deleted (no confirmation returned).");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to delete task {taskID}: {ex.Message}");
        }
    }

    public static async Task<int> AddItem(TaskItem newItem)
    {
        var supabase = await GetSupabaseClient();

        // Prepare the SupabaseTask to insert
        var taskToInsert = new SupabaseTask
        {
            // Id is auto-generated, do not set
            TaskDescription = newItem.TaskDescription,
            IsDone = newItem.IsDone,
            DateOfCreation = newItem.DateOfCreation != default ? newItem.DateOfCreation : DateTime.UtcNow,
            DueDate = newItem.DueDate,
            CurrentIntProgress = newItem.CurrentIntProgress,
            MaxIntProgress = newItem.MaxIntProgress,
            OwnerUserID = newItem.UserOwner?.Id ?? CurrentUser?.Id, // make sure UserOwner.Id matches your User.Id type
            TaskTypeString = TaskTypeHelper.GetStringFromTaskType(newItem.TaskType),
            TaskTitle = newItem.Title 
        };

        try
        {
            var response = await supabase
                .From<SupabaseTask>()
                .Insert(taskToInsert);

            if (response.Models.Count > 0)
            {
                var insertedTask = response.Models[0];
                return insertedTask.Id; // Return the newly generated task ID
            }
            else
            {
                throw new InvalidOperationException("Insert failed: no task returned.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to insert task: {ex.Message}");
            return -1; // Indicate failure
        }
    }

    public static async Task UpdateTaskInfoAsync(int taskId, string newTaskTitle, string newTaskDescription, DateTime? newDueDate, int? newMaxProgress)
    {
        var supabase = await GetSupabaseClient();

        try
        {
            // ✅ Step 1: Get the existing task
            var existingResponse = await supabase
                .From<SupabaseTask>()
                .Where(t => t.Id == taskId)
                .Get();

            var existingTask = existingResponse.Models.FirstOrDefault();

            if (existingTask == null)
            {
                Console.WriteLine($"Task with ID {taskId} not found.");
                return;
            }

            // ✅ Step 2: Update only the fields you want to change
            existingTask.TaskTitle = newTaskTitle;
            existingTask.TaskDescription = newTaskDescription;
            existingTask.DueDate = newDueDate;
            existingTask.MaxIntProgress = newMaxProgress;

            // ✅ Step 3: Save the updated task
            var updateResponse = await supabase
                .From<SupabaseTask>()
                .Where(t => t.Id == taskId)
                .Update(existingTask);

            if (updateResponse.Models.Count > 0)
            {
                Console.WriteLine($"Task {taskId} updated successfully.");
            }
            else
            {
                Console.WriteLine($"Update failed for task ID {taskId}.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating task: {ex.Message}");
        }
    }

    public static async Task CreateTaskRelationshipAsync(int parentTaskId, int childTaskId)
    {
        var supabase = await GetSupabaseClient();

        var existing = await supabase
        .From<TaskRelationship>()
        .Where(x => x.ParentTaskID == parentTaskId && x.ChildTaskID == childTaskId)
        .Get();

        if (existing.Models.Count > 0)
        {
            Console.WriteLine("Relationship already exists. Skipping insert.");
            return;
        }

        var relationship = new TaskRelationship
        {
            ParentTaskID = parentTaskId,
            ChildTaskID = childTaskId
        };

        try
        {
            var response = await supabase
                .From<TaskRelationship>()
                .Insert(relationship);

            Console.WriteLine($"Created task relationship: {parentTaskId} → {childTaskId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating task relationship: {ex.Message}");
        }
    }

    public static async Task RemoveTaskRelationship(int parentTaskId, int childTaskId)
    {
        var supabase = await GetSupabaseClient();

        try
        {
            await supabase
                .From<TaskRelationship>()
                .Where(t => t.ParentTaskID == parentTaskId && t.ChildTaskID == childTaskId)
                .Delete();

            Console.WriteLine($"Removed task relationship: {parentTaskId} → {childTaskId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error removing task relationship: {ex.Message}");
        }
    }




}