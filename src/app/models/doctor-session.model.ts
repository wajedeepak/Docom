import {DoctorQueueNumber} from "./doctor-queue-number.model";
import { SessionStatus } from  "./session-status.model";

export class DoctorSession {
    sessionId?: string;
    status?: SessionStatus;
    queue?: DoctorQueueNumber[];
    currentNumber?: DoctorQueueNumber;
};
