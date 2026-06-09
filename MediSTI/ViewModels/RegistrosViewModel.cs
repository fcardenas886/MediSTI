using MediSTI.Data;
using MediSTI.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace MediSTI.ViewModels
{
    public class RegistrosViewModel
    {
        private readonly DatabaseService _db;

        public ObservableCollection<Registro> Registros { get; set; } = new();

        public RegistrosViewModel(DatabaseService db)
        {
            _db = db;
        }

        

       

        
    }
}
