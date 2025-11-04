
using System.Runtime.InteropServices;
using System.Text;

namespace PACImport
{
    public class PACWrap
    {
        //驱动器操作模式(控制方式)定义
        public const int  OM_PUL_DIR		=   0;	// 脉冲/方向(PUL/DIR)
        public const int  OM_CW_CCW		    =   1;	// 正脉冲/反脉冲(CW/CCW)
        public const int  OM_ENCODER		=   2;	// 编码器跟随
        public const int  OM_PWM_DIR		=   3;	// 开环脉宽/方向调速
        public const int  OM_PWM			=   4;	// 开环单脉宽调速，50%时停止
        public const int  OM_NETWORK		=   5;	// 网络控制(Modbus-RTU)
        public const int  OM_STANDALONE     =   6;	// 独立运行

        //伺服报警代号定义PAC_GetErrorCode()
        public const int  EC_NORMAL	            =	0;	//正常
        public const int  EC_MOTOR_DISABLED	    =	1;	//收到运动指令，但电机未使能
        public const int  EC_MOTOR_MOVING		=	2;	//收到运动指令，但电机仍在运动
        public const int  EC_WARNING_POS_ERR	=	3;	//跟随误差报警
        public const int  EC_FATAL_POS_ERR		=   4;	//跟随误差故障
        public const int  EC_OVERCURRENT		=	5;	//驱动器过流
        public const int  EC_DRIVER_OVERHEAT	=	6;	//驱动器过热
        public const int  EC_OPERA_DENY  		=	7;	//操作不允许
        public const int  EC_SN_ERROR			=	8;	//序列号错误
        public const int  EC_DRIVER_FAULT		=	9;	//功率元件故障，保留
        public const int  EC_CTRL_CONFLICT		=   10;	//控制方式冲突
        public const int  EC_SERVO_MODE			=   11;	//伺服模式错误
        public const int EC_ENCODER_ERROR = 12;	//编码器无反馈
        public const int EC_MOTOR_ERROR = 13;	//电机断线
        public const int EC_PHASE_ERROR = 14;	//相位初始化失败(直线驱动器)
        public const int EC_OVER_SPEED = 15;	//超速(直线驱动器)
        public const int EC_CONT_CUR_PROTECT = 16;	//持续电流保护


        //伺服模式定义PAC_GetServoMode()
        public const int  SM_POS				=   0;	//位置模式
        public const int  SM_VEL				=   1;	//速度模式
        public const int  SM_TQ				    =   2;	//力矩模式
        public const int  SM_SOFTLANDING		=   3;	//软着陆模式

        //输入引脚定义PAC_GetInput()
        public const int  PIN_PULSE		        =   0;	//脉冲信号引脚
        public const int  PIN_DIR			    =   1;	//方向信号引脚
        public const int  PIN_ENABLE		    =   2;	//使能信号引脚
        public const int  PIN_ACLR		        =   3;	//清除报警信号引脚

        //运动方向定义
        public const int  DIR_POSITIVE	        =   0;	//正方向
        public const int DIR_NEGATIVE           =   1;	//负方向


        //函数功能：连接驱动器, 会自动搜索COM端口号（默认：主机以波特率 115200 bps 进行通信）
        //注意： 所有从机(驱动器)刚上电时，从机波特率为：115200 bps
        //输入参数：btAddr为驱动器地址编号，任意总线网络中，任意一个存在的地址即可！
        //返回值：  为TRUE时，函数调用成功
        [DllImport("PAC.dll",CallingConvention=CallingConvention.Cdecl)]
        public extern static bool PAC_AutoConnect(byte btAddr);

        //函数功能：连接驱动器, 会自动搜索COM端口号（需要指定主机的波特率来通信）
        //注意： 从机(驱动器)设定的波特率，必须和主机设定的波特率一致，才能连接上！
        //输入参数：ulBaudrate为波特率，通常设置：115200，57600，38400，19200
        //          btAddr为驱动器地址编号，任意总线网络中，任意一个存在的地址即可！
        //返回值：  为TRUE时，函数调用成功
        [DllImport("PAC.dll",CallingConvention=CallingConvention.Cdecl)]
        public extern static bool PAC_AutoConnect2(uint ulBaudrate, byte btAddr);


        //函数功能：修改所有从机以及主机的波特率，
        //注意事项：
        //    1.必须先调用连接函数连接上后，才能调用此函数修改波特率；
        //    2.该函数最多需要250ms的时间才能返回, 如果造成界面卡顿，请在新的线程中调用！
        //输入参数：ulBaudrate为波特率，通常设置：115200，57600，38400，19200
        //返回值：  为TRUE时，函数调用成功
        [DllImport("PAC.dll",CallingConvention=CallingConvention.Cdecl)]
        public extern static bool PAC_ChangeBaudrate(uint ulNewBaudrate);


        //函数功能：获取主机的当前波特率，
        //输入参数：pulBaudrate=待获取波特率的变量地址
        //返回值：  为TRUE时，函数调用成功
        [DllImport("PAC.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static bool PAC_GetMasterBaudrate(out uint pulBaudrate);

        //函数功能：连接驱动器, 需手动指定COM端口号（主机默认的波特率：115200 bps）
        //注意： 所有从机(驱动器)刚上电时，从机波特率为：115200 bps
        //输入参数：btCom为串口号，btAddr为驱动器地址编号，任意总线网络中，任意一个存在的地址即可！
        //返回值：  为TRUE时，函数调用成功
        [DllImport("PAC.dll",CallingConvention=CallingConvention.Cdecl)]
        public extern static bool PAC_ManualConnect(byte btCom, byte btAddr);

        //函数功能：连接驱动器, 需手动指定COM端口号，以及主机波特率
        //注意： 从机(驱动器)设定的波特率，必须和主机设定的波特率一致，才能连接上！
        //输入参数：ulBaudrate为波特率，通常设置：115200，57600，38400，19200
        //          btCom为串口号，btAddr为驱动器地址编号，任意总线网络中，任意一个存在的地址即可！
        //返回值：  为TRUE时，函数调用成功
        [DllImport("PAC.dll",CallingConvention=CallingConvention.Cdecl)]
        public extern static bool PAC_ManualConnect2(byte btCom, uint ulBaudrate, byte btAddr);

        //函数功能：断开驱动器连接
        //输入参数：无
        //返回值：  为TRUE时，函数调用成功
        [DllImport("PAC.dll",CallingConvention=CallingConvention.Cdecl)]
        public extern static bool PAC_Disconnect();


        //函数功能：检测驱动器是否已经连接
        //输入参数：无
        //返回值：  为TRUE时，表示驱动器已连接
        [DllImport("PAC.dll",CallingConvention=CallingConvention.Cdecl)]
        public extern static bool PAC_IsConnected();


        //函数功能：设置操作模式(控制方式)
        //输入参数：btAddr为驱动器地址，ucMode为待设置的操作模式,可选取的值如下：
        //OM_PUL_DIR:    脉冲/方向(PUL/DIR)
        //OM_CW_CCW:     正脉冲/反脉冲(CW/CCW)
        //OM_ENCODER:    编码器跟随
        //OM_PWM_DIR:    开环脉宽/方向调速
        //OM_PWM:        开环单脉宽调速，50%时停止
        //OM_NETWORK:    RS485总线控制(Modbus-RTU)
        //OM_STANDALONE: 独立运行
        //返回值：  为TRUE时，函数调用成功
        [DllImport("PAC.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static bool PAC_SetOpMode(byte btAddr, byte ucMode);


        //函数功能：设置运行速度
        //输入参数：btAddr为驱动器地址，usVel为待设置的速度值，对音圈电机，单位是mm/s，对有刷直流电机，单位是rpm
        //返回值：  为TRUE时，函数调用成功
        [DllImport("PAC.dll",CallingConvention=CallingConvention.Cdecl)]
        public extern static bool PAC_SetVel(byte btAddr, ushort usVel);

        //函数功能：设置运行加速度
        //输入参数：btAddr为驱动器地址，usAcc为待设置的加速度值，对音圈电机，单位是mm/s/s，对有刷直流电机，单位是rpm/s
        //返回值：  为TRUE时，函数调用成功
        [DllImport("PAC.dll",CallingConvention=CallingConvention.Cdecl)]
        public extern static bool PAC_SetAcc(byte btAddr, ushort usAcc);

        //函数功能：设置编码器反馈位置
        //输入参数：btAddr为驱动器地址，nPos为待设置的坐标值
        //返回值：  为TRUE时，函数调用成功
        [DllImport("PAC.dll",CallingConvention=CallingConvention.Cdecl)]
        public extern static bool PAC_SetActPos(byte btAddr, int nPos);

        //函数功能：使能或去使能电机
        //输入参数：btAddr为驱动器地址，bEnable为TRUE时使能，bEnable为FALSE时去使能
        //返回值：  为TRUE时，函数调用成功
        [DllImport("PAC.dll",CallingConvention=CallingConvention.Cdecl)]
        public extern static bool PAC_Enable(byte btAddr, bool bEnable);

        //函数功能：相对运动一段位移
        //输入参数：btAddr为驱动器地址，nRelCounts为相对运动的脉冲数，
        //          bWaitDone为TRUE时，等待到位后，函数才返回;bWaitDone为FALSE时, 不等待到位，函数立即返回
        //返回值：  为TRUE时，函数调用成功
        [DllImport("PAC.dll",CallingConvention=CallingConvention.Cdecl)]
        public extern static bool PAC_MoveRel(byte btAddr, int nRelCounts, bool bWaitDone);

        //函数功能：绝对运动到指定的坐标
        //输入参数：btAddr为驱动器地址，nPos为要运动到的坐标，
        //          bWaitDone为TRUE时，等待到位后，函数才返回;bWaitDone为FALSE时, 不等待到位，函数立即返回
        //返回值：  为TRUE时，函数调用成功
        [DllImport("PAC.dll",CallingConvention=CallingConvention.Cdecl)]
        public extern static bool PAC_MoveAbs(byte btAddr, int nPos, bool bWaitDone);

        //函数功能：设置伺服模式
        //输入参数：btAddr为驱动器地址，ucMode为待设置的伺服模式
        //          ucMode等于SM_POS时：位置模式；ucMode等于SM_VEL时：速度模式；ucMode等于SM_POS时：力矩模式。
        //返回值：  为TRUE时，函数调用成功
        [DllImport("PAC.dll",CallingConvention=CallingConvention.Cdecl)]
        public extern static bool PAC_SetServoMode(byte btAddr, byte ucMode);

        //函数功能：速度模式下，设置命令速度
        //输入参数：btAddr为驱动器地址，sVel为待设置的速度值，正数表示正转，负数表示反转
        //对音圈电机，单位是mm/s，对有刷直流电机，单位是rpm
        //返回值：  为TRUE时，函数调用成功
        [DllImport("PAC.dll",CallingConvention=CallingConvention.Cdecl)]
        public extern static bool PAC_CmdVel(byte btAddr, short sVel);

        //函数功能：电流模式下，设置命令电流
        //输入参数：btAddr为驱动器地址，sCurrent为电流值，正数表示输出正电流，负数表示输出负电流；取值范围：-2047 ~ +2047
        //返回值：  为TRUE时，函数调用成功
        [DllImport("PAC.dll",CallingConvention=CallingConvention.Cdecl)]
        public extern static bool PAC_CmdCur(byte btAddr, short sCurrent);

        //函数功能：电流模式下，设置电流缓慢升降
        //输入参数：btAddr为驱动器地址，sStartCur为起始电流，sEndCur为终止电流，正负代表方向；电流取值范围：-2047 ~ +2047
        //                usTime为起始电流到终止电流的过渡时间，单位为ms; 
        //                bWaitDone为是否等待完成，TRUE=阻塞至达到终止电流，FALSE=函数立即返回
        //返回值：  为TRUE时，函数调用成功
        [DllImport("PAC.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static bool PAC_CmdCurRamp(byte btAddr, short sStartCur, short sEndCur, ushort usTime, bool bWaitDone);


        //函数功能：搜索编码器的Z相(INDEX)
        //输入参数：btAddr为驱动器地址，usVel为搜索速度，usAcc为搜索加速度，dir为搜索方向,0为正方向，1为负方向，bWaitDone为TRUE时，表示等待搜索完成；
        //返回值：  为TRUE时，函数调用成功
        [DllImport("PAC.dll",CallingConvention=CallingConvention.Cdecl)]
        public extern static bool PAC_FindIndex(byte btAddr, ushort usVel, ushort usAcc, byte dir, bool bWaitDone);

        //函数功能：搜索机械硬限位
        //输入参数：btAddr为驱动器地址，usCurrent为搜索电流，取值范围：-2047 ~ +2047，正负表示方向，bWaitDone为TRUE时，表示等待搜索完成；
        //返回值：  为TRUE时，函数调用成功
        [DllImport("PAC.dll",CallingConvention=CallingConvention.Cdecl)]
        public extern static bool PAC_FindHardLimit(byte btAddr, short usCurrent, bool bWaitDone);

        //函数功能：软着陆
        //输入参数：btAddr为驱动器地址，usVel为着陆速度，单位是mm/s，对有刷直流电机，单位是rpm，dir为着陆方向，0为正方向，1为负方向;
        //          usCurrent为电流(力)限制值, 且着陆时维持输出此值，取值范围1~2047，
        //			bWaitDone为TRUE时，表示等待着陆完成；
        //返回值：  为TRUE时，函数调用成功
        //注意：    着陆完成后，若要让电机抬起，则要先调用PAC_SetServoMode函数切换到位置模式，再调用运动函数！
        [DllImport("PAC.dll",CallingConvention=CallingConvention.Cdecl)]
        public extern static bool PAC_SoftLanding(byte btAddr, ushort usVel, byte dir, ushort usCurrent, bool bWaitDone);

        //函数功能：读取驱动器的输入引脚信号电平
        //输入参数：btAddr为驱动器地址，pin为引脚编号，详见顶部引脚定义，pbInputLevel为返回引脚电平的指针，TRUE为高电平，FALSE为低电平
        //返回值：  为TRUE时，函数调用成功
        [DllImport("PAC.dll",CallingConvention=CallingConvention.Cdecl)]
        public extern static bool PAC_GetInputPin(byte btAddr, ushort pin, out bool pbInputLevel);

        //函数功能：获取当前操作模式(控制方式)
        //输入参数：btAddr为驱动器地址，pbtMode为返回的操作模式的指针
        //返回值：  为TRUE时，函数调用成功
        [DllImport("PAC.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static bool PAC_GetOpMode(byte btAddr, out byte pbtMode);

        //函数功能：获取当前伺服模式
        //输入参数：btAddr为驱动器地址，pbtMode为返回的伺服模式的指针
        //返回值：  为TRUE时，函数调用成功
        [DllImport("PAC.dll",CallingConvention=CallingConvention.Cdecl)]
        public extern static bool PAC_GetServoMode(byte btAddr, out byte pbtMode);

        //函数功能：获取编码器实际位置
        //输入参数：btAddr为驱动器地址，pnPos为返回的实际位置的指针
        //返回值：  为TRUE时，函数调用成功
        [DllImport("PAC.dll",CallingConvention=CallingConvention.Cdecl)]
        public extern static bool PAC_GetActPos(byte btAddr, out int pnPos);

        //函数功能：获取上次的给定位置
        //输入参数：btAddr为驱动器地址，pnPos为返回的给定位置的指针
        //返回值：  为TRUE时，函数调用成功
        [DllImport("PAC.dll",CallingConvention=CallingConvention.Cdecl)]
        public extern static bool PAC_GetRefPos(byte btAddr, out int pnPos);

        //函数功能：获取(报警)故障代码
        //输入参数：btAddr为驱动器地址，pusErrorCode为返回的错误代码的指针
        //返回值：  为TRUE时，函数调用成功
        [DllImport("PAC.dll",CallingConvention=CallingConvention.Cdecl)]
        public extern static bool PAC_GetErrorCode(byte btAddr, out ushort pusErrorCode);

        //函数功能：清除(故障)报警
        //输入参数：btAddr为驱动器地址，
        //返回值：  为TRUE时，函数调用成功
        [DllImport("PAC.dll",CallingConvention=CallingConvention.Cdecl)]
        public extern static bool PAC_ClearError(byte btAddr);

        //函数功能：判断是否运动到位
        //输入参数：btAddr为驱动器地址，pbInPos为返回的到位状态的指针，TRUE表示到位，FALSE表示没到位
        //返回值：  为TRUE时，函数调用成功
        [DllImport("PAC.dll",CallingConvention=CallingConvention.Cdecl)]
        public extern static bool PAC_IsInPosition(byte btAddr, out bool pbInPos);

        //函数功能：判断是否已经使能
        //输入参数：btAddr为驱动器地址，pbEnabled为返回的使能状态的指针，TRUE表示已使能，FALSE表示未使能
        //返回值：  为TRUE时，函数调用成功
        [DllImport("PAC.dll",CallingConvention=CallingConvention.Cdecl)]
        public extern static bool PAC_IsEnabled(byte btAddr, out bool pbEnabled);

        //函数功能：获取实际速度
        //输入参数：btAddr为驱动器地址，psActVel为返回的实际速度的指针
        //返回值：  为TRUE时，函数调用成功
        [DllImport("PAC.dll",CallingConvention=CallingConvention.Cdecl)]
        public extern static bool PAC_GetActVel(byte btAddr, out short psActVel);

        //函数功能：获取实际电流
        //输入参数：btAddr为驱动器地址，pusActCur为返回的实际电流的指针，电流值的范围是：0~4095
        //返回值：  为TRUE时，函数调用成功
        [DllImport("PAC.dll", CallingConvention=CallingConvention.Cdecl)]
        public extern static bool PAC_GetActCur(byte btAddr, out ushort pusActCur);

        //-----------------------二轴或三轴联动控制函数-----------------------------------//

        //函数功能：设置要联动轴的驱动器地址，该函数必须在其它联动函数之前调用！
        //输入参数：btGroup为联动组号(取值0~84)， 例如：在XYT对位平台的应用中，组号用于区分控制的是哪一个对位平台；
        //			btAddrX为X轴驱动器地址，btAddrY为Y轴驱动器地址，btAddrZ为Z轴驱动器地址, 
        //          注意，参与联动的驱动器地址不能为0， 若设为0表示不参与联动。例如：Z不参与联动，则btAddrZ设置为0
        //返回值：  为TRUE时，函数调用成功	
        [DllImport("PAC.dll",CallingConvention=CallingConvention.Cdecl)]
        public extern static bool PAC_LinkSetAxes(byte btGroup, byte btAddrX, byte btAddrY, byte btAddrZ);


        //函数功能：使能或释放要联动的所有轴
        //输入参数：btGroup为联动组号(取值0~84)，bEnable为TRUE时使能，bEnable为FALSE时释放，
        //返回值：  为TRUE时，函数调用成功
        [DllImport("PAC.dll",CallingConvention=CallingConvention.Cdecl)]
        public extern static bool PAC_LinkEnable(byte btGroup, bool bEnable);

        //函数功能：设置联动轴的运行速度
        //输入参数：btGroup为联动组号(取值0~84)，usVel为待设置的速度，对音圈电机，单位是mm/s/s，对有刷直流电机，单位是rpm
        //返回值：  为TRUE时，函数调用成功
        [DllImport("PAC.dll",CallingConvention=CallingConvention.Cdecl)]
        public extern static bool PAC_LinkSetVel(byte btGroup, ushort usVel);

        //函数功能：设置联动轴的运行加速度
        //输入参数：btGroup为联动组号(取值0~84)，usAcc为待设置的加速度，对音圈电机，单位是mm/s/s，对有刷直流电机，单位是rpm/s
        //返回值：  为TRUE时，函数调用成功
        [DllImport("PAC.dll",CallingConvention=CallingConvention.Cdecl)]
        public extern static bool PAC_LinkSetAcc(byte btGroup, ushort usAcc);

        //函数功能：设置联动轴的位置坐标
        //输入参数：btGroup为联动组号(取值0~84)，nPosX为X轴坐标，nPosY为Y轴坐标，nPosZ为Z轴坐标，
        //返回值：  为TRUE时，函数调用成功
        [DllImport("PAC.dll",CallingConvention=CallingConvention.Cdecl)]
        public extern static bool PAC_LinkSetActPos(byte btGroup, int nPosX, int nPosY, int nPosZ);

        //函数功能：联动相对运动
        //输入参数：btGroup为联动组号(取值0~84)，nRelCountsX, nRelCountsY, nRelCountsZ分别为联动三轴的脉冲数。
        //          注意：若该轴不参与联动，则传入的脉冲数将被忽略，该轴不会产生运动；    
        //          bWaitDone为TRUE时，等待到位后，函数才返回;bWaitDone为FALSE时, 不等待到位，函数立即返回
        //返回值：  为TRUE时，函数调用成功
        [DllImport("PAC.dll",CallingConvention=CallingConvention.Cdecl)]
        public extern static bool PAC_LinkMoveRel(byte btGroup, int nRelCountsX, int nRelCountsY, int nRelCountsZ, bool bWaitDone);

        //函数功能：联动绝对运动
        //输入参数：btGroup为联动组号(取值0~84)，nPosX, nPosY, nPosZ分别为联动三轴要运动到的位置坐标
        //          注意：若该轴不参与联动，则传入的位置坐标将被忽略，该轴不会产生运动；
        //          bWaitDone为TRUE时，等待到位后，函数才返回;bWaitDone为FALSE时, 不等待到位，函数立即返回
        //返回值：  为TRUE时，函数调用成功
        [DllImport("PAC.dll",CallingConvention=CallingConvention.Cdecl)]
        public extern static bool PAC_LinkMoveAbs(byte btGroup, int nPosX, int nPosY, int nPosZ, bool bWaitDone);

        //函数功能：判断联动轴是否运动到位
        //输入参数：btGroup为联动组号(取值0~84)，pbInPos为返回的到位状态的指针，TRUE表示到位，FALSE表示没到位
        //返回值：  为TRUE时，函数调用成功
        [DllImport("PAC.dll",CallingConvention=CallingConvention.Cdecl)]
        public extern static bool PAC_LinkIsInPosition(byte btGroup, out bool pbInPos);
        //------------------------------------------------------------------------------------
        //-------------------2021-04-27或更新的固件，才支持以下回原点函数-----------------------------

        //函数功能：设置联动组的回原点模式
        //输入参数：btGroup为联动组号(取值0~84)， usMode 为原点模式，0~5（根据不同驱动器）
        //usMode:
        //0 = 先往负方向软着陆，然后往正方向找零位，再运动到偏移位置；
        //1 = 先往正方向软着陆，然后往负方向找零位，再运动到偏移位置；
        //2 = 先往负方向软着陆，然后往正方向运动到偏移位置；
        //3 = 先往正方向软着陆，然后往负方向运动到偏移位置；
        //返回值：  为TRUE时，函数调用成功
        [DllImport("PAC.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static bool PAC_LinkSetHomingMode(byte btGroup, ushort usMode);

        //函数功能：设置联动组的回原点速度
        //输入参数：btGroup为联动组号(取值0~84)， usVel 为回原点速度 ，mm/s
        //返回值：  为TRUE时，函数调用成功
        [DllImport("PAC.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static bool PAC_LinkSetHomingVel(byte btGroup, ushort usVel);

        //函数功能：设置联动组的回原点速度
        //输入参数：btGroup为联动组号(取值0~84)， usAcc 为回原点加速度，mm/s/s
        //返回值：  为TRUE时，函数调用成功
        [DllImport("PAC.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static bool PAC_LinkSetHomingAcc(byte btGroup, ushort usAcc);

        //函数功能：设置联动组的回原点电流限制值
        //输入参数：btGroup为联动组号(取值0~84)， usCur 为回原点电流限制值，0~2047
        //返回值：  为TRUE时，函数调用成功
        [DllImport("PAC.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static bool PAC_LinkSetHomingCur(byte btGroup, ushort usCur);

        //函数功能：设置联动组的回原点位置偏移
        //输入参数：btGroup为联动组号(取值0~84)， iOffsetX,iOffsetY,iOffsetZ 为联动组的三个轴回原点位置偏移，counts
        //返回值：  为TRUE时，函数调用成功
        [DllImport("PAC.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static bool PAC_LinkSetHomingOffset(byte btGroup, int iOffsetX, int iOffsetY, int iOffsetZ);

        //函数功能：联动组启动回原点
        //输入参数：btGroup为联动组号(取值0~84)
        //返回值：  为TRUE时，函数调用成功
        [DllImport("PAC.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static bool PAC_LinkStartHoming(byte btGroup);

        //函数功能：检测联动组 回原点是否执行完成（在PAC_StartHoming后调用）
        //输入参数：btGroup为联动组号(取值0~84)， bHomingDone 为待获取脚本完成标志的变量指针, TRUE = 执行完成
        //返回值：  为TRUE时，函数调用成功
        [DllImport("PAC.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static bool PAC_LinkIsHomingDone(byte btGroup, out bool bHomingDone);

        //-------------------以下是设置或获取PID参数的函数接口--------------------------------

        //函数功能：设置比例增益Kp
        //输入参数：btAddr为驱动器地址，Kp为待设置的数值
        //返回值：  为TRUE时，函数调用成功
        [DllImport("PAC.dll",CallingConvention=CallingConvention.Cdecl)]
        public extern static bool PAC_SetKp(byte btAddr, ushort Kp);

        //函数功能：设置微分增益Kd
        //输入参数：btAddr为驱动器地址，Kd为待设置的数值
        //返回值：  为TRUE时，函数调用成功
        [DllImport("PAC.dll",CallingConvention=CallingConvention.Cdecl)]
        public extern static bool PAC_SetKd(byte btAddr, ushort Kd);

        //函数功能：设置速度前馈增益Kvff
        //输入参数：btAddr为驱动器地址，Kvff为待设置的数值
        //返回值：  为TRUE时，函数调用成功
        [DllImport("PAC.dll",CallingConvention=CallingConvention.Cdecl)]
        public extern static bool PAC_SetKvff(byte btAddr, ushort Kvff);

        //函数功能：设置积分增益Ki
        //输入参数：btAddr为驱动器地址，Ki为待设置的数值
        //返回值：  为TRUE时，函数调用成功
        [DllImport("PAC.dll",CallingConvention=CallingConvention.Cdecl)]
        public extern static bool PAC_SetKi(byte btAddr, ushort Ki);

        //函数功能：设置积分模式Kim
        //输入参数：btAddr为驱动器地址，Kim为待设置的数值
        //返回值：  为TRUE时，函数调用成功
        [DllImport("PAC.dll",CallingConvention=CallingConvention.Cdecl)]
        public extern static bool PAC_SetKim(byte btAddr, ushort Kim);

        //函数功能：设置稳态比例增益Kc
        //输入参数：btAddr为驱动器地址，Kc为待设置的数值
        //返回值：  为TRUE时，函数调用成功
        [DllImport("PAC.dll",CallingConvention=CallingConvention.Cdecl)]
        public extern static bool PAC_SetKc(byte btAddr, ushort Kc);

        //函数功能：永久保存数据/运动程序(脚本) 到驱动器
        //输入参数：btAddr为驱动器地址，
        //返回值：  为TRUE时，函数调用成功
        [DllImport("PAC.dll",CallingConvention=CallingConvention.Cdecl)]
        public extern static bool PAC_Save(byte btAddr);

        //函数功能：获取比例增益Kp
        //输入参数：btAddr为驱动器地址，pKp为待获取的数据变量的指针
        //返回值：  为TRUE时，函数调用成功
        [DllImport("PAC.dll",CallingConvention=CallingConvention.Cdecl)]
        public extern static bool PAC_GetKp(byte btAddr, out ushort pKp);

        //函数功能：获取微分增益Kd
        //输入参数：btAddr为驱动器地址，pKd为待获取的数据变量的指针
        //返回值：  为TRUE时，函数调用成功
        [DllImport("PAC.dll",CallingConvention=CallingConvention.Cdecl)]
        public extern static bool PAC_GetKd(byte btAddr, out ushort pKd);

        //函数功能：获取速度前馈增益Kvff
        //输入参数：btAddr为驱动器地址，pKvff为待获取的数据变量的指针
        //返回值：  为TRUE时，函数调用成功
        [DllImport("PAC.dll",CallingConvention=CallingConvention.Cdecl)]
        public extern static bool PAC_GetKvff(byte btAddr, out ushort pKvff);

        //函数功能：获取积分增益Ki
        //输入参数：btAddr为驱动器地址，pKi为待获取的数据变量的指针
        //返回值：  为TRUE时，函数调用成功
        [DllImport("PAC.dll",CallingConvention=CallingConvention.Cdecl)]
        public extern static bool PAC_GetKi(byte btAddr, out ushort pKi);

        //函数功能：获取积分模式Kim
        //输入参数：btAddr为驱动器地址，pKim为待获取的数据变量的指针
        //返回值：  为TRUE时，函数调用成功
        [DllImport("PAC.dll",CallingConvention=CallingConvention.Cdecl)]
        public extern static bool PAC_GetKim(byte btAddr, out ushort pKim);

        //函数功能：获取稳态比例增益Kc
        //输入参数：btAddr为驱动器地址，pKc为待获取的数据变量的指针
        //返回值：  为TRUE时，函数调用成功
        [DllImport("PAC.dll",CallingConvention=CallingConvention.Cdecl)]
        public extern static bool PAC_GetKc(byte btAddr, out ushort pKc);


        //////////提供一个MODBUS底层接口，方便测试, 务必谨慎调用//////////////////////////////
        //函数功能：设置MODBUS线圈位
        //输入参数：btAddr为驱动器地址，usCoilIndex为MODBUS线圈编号, bOnOff为线圈状态，TRUE=闭合
        //返回值：  为TRUE时，函数调用成功
        [DllImport("PAC.dll",CallingConvention=CallingConvention.Cdecl)]
        public extern static bool PAC_SetCoilBit(byte btAddr, ushort usCoilIndex, bool bOnOff);

        //函数功能：获取MODBUS线圈位
        //输入参数：btAddr为驱动器地址，usCoilIndex为MODBUS线圈编号, pbOnOff为线圈状态，TRUE=闭合
        //返回值：  为TRUE时，函数调用成功
        [DllImport("PAC.dll",CallingConvention=CallingConvention.Cdecl)]
        public extern static bool PAC_GetCoilBit(byte btAddr, ushort usCoilIndex, out bool pbOnOff);

        //函数功能：设置MODBUS多个线圈位
        //输入参数：btAddr为驱动器地址，usCoilIndex为MODBUS起始线圈编号, usCoils为线圈个数，pubtOnOff为多个线圈状态，相应位为1表示ON
        //返回值：  为TRUE时，函数调用成功
        [DllImport("PAC.dll",CallingConvention=CallingConvention.Cdecl)]
        public extern static bool PAC_SetMultiCoils(byte btAddr, ushort usCoilIndex, ushort usCoils, out byte  pubtOnOff);

        //函数功能：获取MODBUS多个线圈位
        //输入参数：btAddr为驱动器地址，usCoilIndex为MODBUS起始线圈编号, usCoils为线圈个数，pubtOnOff为多个线圈状态，相应位为1表示ON
        //返回值：  为TRUE时，函数调用成功
        [DllImport("PAC.dll",CallingConvention=CallingConvention.Cdecl)]
        public extern static bool PAC_GetMultiCoils(byte btAddr, ushort usCoilIndex, ushort usCoils, out byte  pubtOnOff);

        //函数功能：设置MODBUS保持寄存器的值
        //输入参数：btAddr为驱动器地址，usRegIndex为MODBUS保持寄存器编号, usRegValue为保持寄存器的值
        //返回值：  为TRUE时，函数调用成功
        [DllImport("PAC.dll",CallingConvention=CallingConvention.Cdecl)]
        public extern static bool PAC_SetHoldingReg(byte btAddr, ushort usRegIndex, ushort usRegValue);

        //函数功能：设置多个MODBUS保持寄存器的值
        //输入参数：btAddr为驱动器地址，usRegIndex为MODBUS保持寄存器编号, pusRegValue为待获取数据的指针
        //返回值：  为TRUE时，函数调用成功
        [DllImport("PAC.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static bool PAC_SetMultiHoldingReg(byte btAddr, ushort usRegIndex, byte usCounts, ushort[] pusRegValue);



        //函数功能：获取MODBUS保持寄存器的值
        //输入参数：btAddr为驱动器地址，usRegIndex为MODBUS保持寄存器编号, pusRegValue为待获取数据的指针
        //返回值：  为TRUE时，函数调用成功
        [DllImport("PAC.dll",CallingConvention=CallingConvention.Cdecl)]
        public extern static bool PAC_GetHoldingReg(byte btAddr, ushort usRegIndex, out ushort pusRegValue);

        //函数功能：获取多个MODBUS保持寄存器的值
        //输入参数：btAddr为驱动器地址，usRegIndex为MODBUS保持寄存器编号, pusRegValue为待获取数据的指针
        //返回值：  为TRUE时，函数调用成功
        [DllImport("PAC.dll",CallingConvention=CallingConvention.Cdecl)]
        public extern static bool PAC_GetMultiHoldingReg(byte btAddr, ushort usRegIndex, byte usCounts,  ushort[] pusRegValue);


        //函数功能：获取MODBUS输入寄存器的值
        //输入参数：btAddr为驱动器地址，usRegIndex为MODBUS输入寄存器编号, pusRegValue为待获取数据的指针
        //返回值：  为TRUE时，函数调用成功
        [DllImport("PAC.dll",CallingConvention=CallingConvention.Cdecl)]
        public extern static bool PAC_GetInputReg(byte btAddr, ushort usRegIndex, out ushort pusRegValue);

        //函数功能：获取多个MODBUS输入寄存器的值
        //输入参数：btAddr为驱动器地址，usRegIndex为MODBUS输入寄存器编号, pusRegValue为待获取数据的指针
        //返回值：  为TRUE时，函数调用成功
        [DllImport("PAC.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static bool PAC_GetMultiInputReg(byte btAddr, ushort usRegIndex, byte usCounts,  ushort[] pusRegValue);


        //-----------------------------------------------------------------------------------------------

        //-------------------------------以下是脚本下载及运行函数接口----------------------------------------------

        //函数功能：清除脚本
        //输入参数：btAddr为驱动器地址
        //返回值：  为TRUE时，函数调用成功
        [DllImport("PAC.dll",CallingConvention=CallingConvention.Cdecl)]
        public extern static bool PAC_ClearScript(byte btAddr);

        //函数功能：运行脚本
        //输入参数：btAddr为驱动器地址
        //返回值：  为TRUE时，函数调用成功
        [DllImport("PAC.dll",CallingConvention=CallingConvention.Cdecl)]
        public extern static bool PAC_RunScript(byte btAddr);

        //函数功能：停止脚本，
        //说明：如果有无限循环指令，则运行完循环前的最后一条指令才会停止；
        //输入参数：btAddr为驱动器地址
        //返回值：  为TRUE时，函数调用成功
        [DllImport("PAC.dll",CallingConvention=CallingConvention.Cdecl)]
        public extern static bool PAC_StopScript(byte btAddr);


        //函数功能：下载一条脚本指令
        //输入参数：btAddr为驱动器地址，ucLine为脚本指令的行号，从0行开始，最多到99， pcCmd 为指向脚本指令的字符串的指针,字符串采用ASCII编码
        //返回值：  为TRUE时，函数调用成功
        [DllImport("PAC.dll",CallingConvention=CallingConvention.Cdecl,CharSet=CharSet.Ansi)]
        public extern static bool PAC_DownloadScriptCmd(byte btAddr, byte ucLine, string pcCmd);


        //函数功能：下载脚本文件，可以包含多条脚本指令；
        //输入参数：btAddr为驱动器地址，pcFileName 为指向脚本文件路径名的字符串指针,字符串采用ASCII编码
        //返回值：  为TRUE时，函数调用成功
        [DllImport("PAC.dll",CallingConvention=CallingConvention.Cdecl,CharSet=CharSet.Ansi)]
        public extern static bool PAC_DownloadScriptFile(byte btAddr, string pcFileName);


        //函数功能：上传一条脚本指令
        //输入参数：btAddr为驱动器地址，ucLine为脚本指令的行号，从0行开始，最多到99， pcCmd 为指向脚本指令的字符串的指针,字符串采用ASCII编码
        //返回值：  为TRUE时，函数调用成功
        [DllImport("PAC.dll",CallingConvention=CallingConvention.Cdecl,CharSet=CharSet.Ansi)]
        public extern static bool PAC_UploadScriptCmd(byte btAddr, byte ucLine, StringBuilder pcCmd);

        //函数功能：上传脚本并保存到文件
        //输入参数：btAddr为驱动器地址，pcFileName 为指向脚本文件路径名的字符串指针,字符串采用ASCII编码
        //返回值：  为TRUE时，函数调用成功
        [DllImport("PAC.dll",CallingConvention=CallingConvention.Cdecl,CharSet=CharSet.Ansi)]
        public extern static bool PAC_UploadScriptFile(byte btAddr, string pcFileName);


        //函数功能：检测脚本是否执行完成
        //说明：如果脚本包含无限循环指令，除非调用PAC_StopScript停止脚本，否则脚本一直执行
        //输入参数：btAddr为驱动器地址， bScriptDone 为待获取脚本完成标志的变量指针, TRUE = 执行完成
        //返回值：  为TRUE时，函数调用成功
        [DllImport("PAC.dll",CallingConvention=CallingConvention.Cdecl)]
        public extern static bool PAC_IsScriptDone(byte btAddr, out bool bScriptDone);


        //-------------------2021-04-27或更新的固件，才支持以下回原点函数-----------------------------

        //函数功能：设置回原点模式
        //输入参数：btAddr为驱动器地址， usMode 为原点模式，0~5（根据不同驱动器）
        //usMode:
        //0 = 先往负方向软着陆，然后往正方向找零位，再运动到偏移位置；
        //1 = 先往正方向软着陆，然后往负方向找零位，再运动到偏移位置；
        //2 = 先往负方向软着陆，然后往正方向运动到偏移位置；
        //3 = 先往正方向软着陆，然后往负方向运动到偏移位置；
        //返回值：  为TRUE时，函数调用成功
        [DllImport("PAC.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static bool PAC_SetHomingMode(byte btAddr, ushort usMode);

        //函数功能：设置回原点速度
        //输入参数：btAddr为驱动器地址， usVel 为回原点速度 ，mm/s
        //返回值：  为TRUE时，函数调用成功
        [DllImport("PAC.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static bool PAC_SetHomingVel(byte btAddr, ushort usVel);

        //函数功能：设置回原点速度
        //输入参数：btAddr为驱动器地址， usAcc 为回原点加速度，mm/s/s
        //返回值：  为TRUE时，函数调用成功
        [DllImport("PAC.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static bool PAC_SetHomingAcc(byte btAddr, ushort usAcc);

        //函数功能：设置回原点电流限制值
        //输入参数：btAddr为驱动器地址， usCur 为回原点电流限制值，0~2047
        //返回值：  为TRUE时，函数调用成功
        [DllImport("PAC.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static bool PAC_SetHomingCur(byte btAddr, ushort usCur);

        //函数功能：设置回原点位置偏移
        //输入参数：btAddr为驱动器地址， iOffset 为回原点位置偏移，counts
        //返回值：  为TRUE时，函数调用成功
        [DllImport("PAC.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static bool PAC_SetHomingOffset(byte btAddr, int iOffset);

        //函数功能：启动回原点
        //输入参数：btAddr为驱动器地址
        //返回值：  为TRUE时，函数调用成功
        [DllImport("PAC.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static bool PAC_StartHoming(byte btAddr);

        //函数功能：检测回原点是否执行完成（在PAC_StartHoming后调用）
        //输入参数：btAddr为驱动器地址， bHomingDone 为待获取脚本完成标志的变量指针, TRUE = 执行完成
        //返回值：  为TRUE时，函数调用成功
        [DllImport("PAC.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static bool PAC_IsHomingDone(byte btAddr, out bool bHomingDone);

    }
}
