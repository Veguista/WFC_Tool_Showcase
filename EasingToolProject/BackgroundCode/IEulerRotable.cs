public interface IEulerRotable
{
    // Because Euler angles are tricky, in order to use them easily we need to treat them as write only.
    // This interface is meant to return a float value for what value needs to be given to the rotation.
    float ReturnRotation();
}
