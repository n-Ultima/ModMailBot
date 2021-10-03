using System;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace Modmail.Common
{
    public class ModmailConfiguration
    {
        private string _Token = null!;
        private ulong[] _OwnerIds;
        private string _Prefix = null!;
        private bool? _AllowMove = false!;
        private ulong _AdminRoleId = default!;
        private ulong _ModRoleId = default!;
        private string _DbConnectionString = null!;
        private bool? _ReplyToTicketsWithoutCommand = false!;
        private string _NewTicketCreationMessage = null!;
        private ulong _MainServerId = default!;
        private ulong _InboxServerId = default!;
        private ulong _ModmailCategoryId = default!;
        private ulong _LogChannelId = default!;
        private bool? _ConfirmThreadCreation = false!;
        
        public ModmailConfiguration()
        {
            LoadModmailConfiguration();
        }

        private void LoadModmailConfiguration()
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("config.json")
                .Build();
            Token = config.GetValue<string>(nameof(Token));
            OwnerIds = config.GetSection(nameof(OwnerIds)).Get<ulong[]>();
            Prefix = config.GetValue<string>(nameof(Prefix));
            AllowMove = config.GetValue<bool>(nameof(AllowMove));
            AdminRoleId = config.GetValue<ulong>(nameof(AdminRoleId));
            ModRoleId = config.GetValue<ulong>(nameof(ModRoleId));
            DbConnectionString = config.GetValue<string>(nameof(DbConnectionString));
            ReplyToTicketsWithoutCommand = config.GetValue<bool>(nameof(ReplyToTicketsWithoutCommand));
            NewTicketCreationMessage = config.GetValue<string>(nameof(NewTicketCreationMessage));
            MainServerId = config.GetValue<ulong>(nameof(MainServerId));
            InboxServerId = config.GetValue<ulong>(nameof(InboxServerId));
            ModmailCategoryId = config.GetValue<ulong>(nameof(ModmailCategoryId));
            LogChannelId = config.GetValue<ulong>(nameof(LogChannelId));
            ConfirmThreadCreation = config.GetValue<bool>(nameof(ConfirmThreadCreation));
        }
        public string Token
        {
            get => _Token;
            set
            {
                if (value == null)
                    throw new NullReferenceException("Token should be defined in config.json");
                _Token = value;
            }
        }

        public ulong[] OwnerIds
        {
            get => _OwnerIds;
            set
            {
                if (!value.Any())
                {
                    throw new NullReferenceException("At least one owner Id should be defined in config.json");
                }
                _OwnerIds = value;
            }
        }

        public string Prefix
        {
            get => _Prefix;
            set
            {
                if (value == null)
                    throw new NullReferenceException("Prefix should be defined in config.json");
                _Prefix = value;
            }
        }

        public bool AllowMove
        {
            get => _AllowMove.Value;
            set => _AllowMove = value;
        }

        public bool ConfirmThreadCreation
        {
            get => _ConfirmThreadCreation.Value;
            set => _ConfirmThreadCreation = value;
        }
        
        public ulong AdminRoleId
        {
            get => _AdminRoleId;
            set
            {
                if (value == default)
                    throw new NullReferenceException("AdminRoleId should be defined in config.json");
                _AdminRoleId = value;
            }
        }
        public ulong ModRoleId
        {
            get => _ModRoleId;
            set
            {
                if (value == default)
                    throw new NullReferenceException("ModRoleId should be defined in config.json");
                _ModRoleId = value;
            }
        }

        public string DbConnectionString
        {
            get => _DbConnectionString;
            set
            {
                if (value == null)
                    throw new NullReferenceException("DbConnectionString should be defined in config.json");
                _DbConnectionString = value;
            }
        }

        public string NewTicketCreationMessage
        {
            get => _NewTicketCreationMessage;
            set
            {
                if (value == null)
                    throw new NullReferenceException("NewTicketCreationMessage should be defined in config.json");
                _NewTicketCreationMessage = value;
            }
        }
        public bool ReplyToTicketsWithoutCommand
        {
            get => _ReplyToTicketsWithoutCommand.Value;
            set => _ReplyToTicketsWithoutCommand = value;
        }

        public ulong MainServerId
        {
            get => _MainServerId;
            set
            {
                if (value == default)
                    throw new NullReferenceException("MainServerId should be defined in config.json");
                _MainServerId = value;
            }
        }

        public ulong InboxServerId
        {
            get => _InboxServerId;
            set
            {
                if (value == default)
                    throw new NullReferenceException("InboxServerId should be defined in config.json");
                _InboxServerId = value;
            }
        }

        public ulong ModmailCategoryId
        {
            get => _ModmailCategoryId;
            set
            {
                if (value == default)
                    throw new NullReferenceException("ModmailCategoryId should be defined in config.json");
                _ModmailCategoryId = value;
            }
        }

        public ulong LogChannelId
        {
            get => _LogChannelId;
            set
            {
                if (value == default)
                    throw new NullReferenceException("LogChannelId should be defined in config.json");
                _LogChannelId = value;
            }
        }
    }
}
