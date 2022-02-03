﻿/************************************************************************************
Copyright : Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.

Your use of this SDK or tool is subject to the Oculus SDK License Agreement, available at
https://developer.oculus.com/licenses/oculussdk/

Unless required by applicable law or agreed to in writing, the Utilities SDK distributed
under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
ANY KIND, either express or implied. See the License for the specific language governing
permissions and limitations under the License.
************************************************************************************/

namespace Oculus.Interaction.Input
{
    /// <summary>
    /// A set of constants that are passed to each child of a Hand modifier tree from the root DataSource.
    /// </summary>
    public class HandDataSourceConfig
    {
        public Handedness Handedness { get; set; }
        public ITrackingToWorldTransformer TrackingToWorldTransformer { get; set; }
        public HandSkeleton HandSkeleton { get; set; }
        public IDataSource<HmdDataAsset, HmdDataSourceConfig> HmdData { get; set; }
    }
}
