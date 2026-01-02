# Serial Communication Protocol

## Frame Format

| Field | Length (Bytes) | Description |
|--------------|--------------|----------------------|
| Frame Header | 2 | Fixed value used to mark the start of a data frame. |
| Command/Msg Type | 1 | Defines the function of this packet, whether it is a command or a response. |
| Data Length | 1 | Byte length of the Data Payload field. |
| Data Payload | 0 - 255 | Actual data content being transmitted, variable length. |
| Checksum | 1 | Used to verify data integrity and prevent transmission errors. |
| Frame Tail | 2 | Fixed value used to mark the end of a data frame. |


## Frame Details

- Frame Header: 0xAA 0x55

- Command/Msg Type
  - PC -> UMH (Command)
    - 0x01: Enable/Disable (Enable or disable the device)
      - Payload:
        - Enable: byte (0x00: Disable, 0x01: Enable)
    - 0x02: Ping (Used for connection test and automatic serial port recognition)
      - Payload:
        - Random Number: byte[1]
    - 0x03: GetStatus (Get device status)
      - Payload: None
    - 0x04: SetStimulation (Set stimulation information)
      - Payload:
        - stimulation_type: byte (0x00: Point, 0x01: Vibration, 0x02: Linear, 0x03: Circular)
        - data:
          - Point: float[3] (position)
          - Vibration: 2*float[3] (vibration start, vibration end)
          - Linear: 2*float[3] (start position, end position)
          - Circular: float[7] (center position, normal vector, radius)
        - strength: float
        - frequency: float
    - 0x05: SetPhases (Set phases of each transducer)
      - Payload:
        - Phases: float[NumTransducer]
  - UMH -> PC (Response)
    - 0x80: ACK (General success response)
    - 0x81: NACK (General failure response)
    - 0x82: Ping_ACK (Specific response to Ping command)
      - For the Ping command (0x04): The PC may send a random number as the data payload, and the UMH must return that same random number unchanged in the Ping_ACK (0x83) response to increase recognition reliability.
      - Payload:
        - Random Number: byte[1]
    - 0x83: Return Status (Return specific status, e.g., temperature)
      - Payload:
        - Voltage: float
        - Temperature: float
        - StimulationType: byte (0x00: Point, 0x01: Vibration, 0x02: Linear, 0x03: Circular)
        - StimulationRefreshDeltaTime: float
        - LoopFreq: float
        - CalibrationMode: byte (0x00: Off, 0x01: On)
        - PlaneMode: byte (0x00: Off, 0x01: On)
    - 0x84: SACK (Stimulation Sent)
      - Payload:
        - NULL
      

    - 0xFF: Error Code (Return detailed error code)
- Data Length: unsigned char (1 byte)
  - Type: unsigned char (1 byte)
  - Range: 0 - 255
  - Description: The value of this byte indicates how many bytes are in the Data Payload that follows. If a command has no data payload (e.g., GetVersion), this byte is 0x00
- Data Payload
  - This field carries the actual data, the content of which depends on the Command/Msg Type.
- Checksum
  - Algorithm: 8-bit Sum Checksum
  - Calculation: Perform unsigned addition on all bytes from Command/Msg Type through the end of Data Payload, then take the lowest 8 bits of the result (i.e., modulo 256).
  - Advantages: Simple to compute, minimal resource usage, ideal for MCUs like STM32, and effectively detects single-byte transmission errors.
  - Example:
    - Command: GetVersion (0x04), no data payload.
    - Bytes to checksum: [Command/Msg Type] [Data Length] [Data Payload]
    - That is: 0x04 + 0x00 = 0x04
    - The checksum byte is therefore 0x04.
- Frame Tail: 0x0D 0x0A
  - Type: unsigned char (2 bytes)
  - Description: Fixed value used to mark the end of a data frame.
