using System.ComponentModel;

namespace CenboGeneral
{
    public class NoteInfo
    {
        /// <summary>
        /// 短息内容
        /// </summary>
        [DisplayName("短息内容")]
        public string NoteContent { get; set; }

        /// <summary>
        /// 短息接收号码(,隔开)
        /// </summary>
        [DisplayName("短息接收号码")]
        public string AddresseeTel { get; set; }

        /// <summary>
        /// 发送时间
        /// </summary>
        [DisplayName("发送时间")]
        public DateTime SendTime { get; set; }

        /// <summary>
        /// 业务系统名字
        /// </summary>
        [DisplayName("业务系统名字")]
        public string AppCode { get; set; }
    }
}
