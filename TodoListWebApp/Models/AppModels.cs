using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace TodoListWebApp.Models
{
    // Entity class for todo entries
    public class Todo
    {
        public int ID { get; set; }
        public string Owner { get; set; }
        public string Description { get; set; }
    }
}