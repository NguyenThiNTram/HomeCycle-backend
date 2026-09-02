using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace HomeCycle.Infrastructure.Persistences.Entities
{
    [Table("Conversation")]
    [Index(
    nameof(UserOneId),
    nameof(UserTwoId),
    Name = "uq_conversation_user_pair",
    IsUnique = true)]
    public partial class Conversation
    {
        [Key]
        public Guid ConversationId { get; set; }

        public Guid UserOneId { get; set; }

        public Guid UserTwoId { get; set; }

        public DateTime LastActivityAt { get; set; }

        public DateTime CreatedAt { get; set; }

        [ForeignKey("UserOneId")]
        [InverseProperty("ConversationUserOnes")]
        public virtual User UserOne { get; set; } = null!;

        [ForeignKey("UserTwoId")]
        [InverseProperty("ConversationUserTwos")]
        public virtual User UserTwo { get; set; } = null!;

        [InverseProperty("Conversation")]
        public virtual ICollection<Negotiation> Negotiations { get; set; } = new List<Negotiation>();

        [InverseProperty("Conversation")]
        public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
    }
}
